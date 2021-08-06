﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace tModPorter.Rewriters {
	public abstract class BaseRewriter {
		protected readonly SemanticModel _model;
		private readonly List<string> _usingList;
		private readonly HashSet<(BaseRewriter rewriter, SyntaxNode originalNode)> _nodesToRewrite;
		private readonly HashSet<(BaseRewriter rewriter, SyntaxToken originalToken)> _tokensToRewrite = new();

		public BaseRewriter(SemanticModel model, List<string> usingList,
			HashSet<(BaseRewriter rewriter, SyntaxNode originalNode)> nodesToRewrite,
			HashSet<(BaseRewriter rewriter, SyntaxToken originalToken)> tokensToRewrite) {
			_model = model;
			_usingList = usingList;
			_nodesToRewrite = nodesToRewrite;
			_tokensToRewrite = tokensToRewrite;
		}

		public virtual RewriterType RewriterType => RewriterType.None;

		/// <summary>
		///     Override this method to queue a modification to a node. The type of node depends on <see cref="RewriterType" />
		/// </summary>
		/// <param name="node">The original node</param>
		/// <returns>Return <see cref="node" /></returns>
		public virtual void VisitNode(SyntaxNode node) { }

		/// <summary>
		///     Override this method to rewrite a node added using <see cref="AddNodeToRewrite" />
		/// </summary>
		/// <param name="node">The node to rewrite</param>
		/// <returns>The rewritten node</returns>
		public virtual SyntaxNode RewriteNode(SyntaxNode node) => node;

		public virtual SyntaxToken RewriteToken(SyntaxToken token) => token;

		protected void AddUsing(string newUsing)
		{
			if (_usingList is null) return;
			
			if (!_usingList.Contains(newUsing.Trim()))
				_usingList.Add(newUsing.Trim());
		}

		protected void AddNodeToRewrite(SyntaxNode node) => _nodesToRewrite?.Add((this, node));
		protected void AddTokenToRewrite(SyntaxToken token) => _tokensToRewrite?.Add((this, token));

		protected bool HasSymbol(SyntaxNode node, out ISymbol symbol) {
			// Try to get the symbol
			try {
				symbol = _model.GetSymbolInfo(node).Symbol;
				return symbol != null;
			}
			catch {
				// This should never be reached, if it is reached, something went horribly wrong
				Console.WriteLine("Symbol not found on node: " + node);
				symbol = null;
				return false;
			}
		}

		protected static bool TryGetAncestorNode<TNode>(SyntaxNode currentNode, [NotNullWhen(true)] out TNode ancestor) where TNode : SyntaxNode
		{
			ancestor = null;
			if (currentNode == null) return false;
			
			IEnumerable<TNode> ancestors = currentNode.Ancestors().OfType<TNode>();
			TNode[] syntaxNodes = ancestors as TNode[] ?? ancestors.ToArray();
			if (syntaxNodes.Length == 0) return false;

			ancestor = syntaxNodes.First();
			return true;
		}
	}
}