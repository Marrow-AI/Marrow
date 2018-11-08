using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedBlueGames.Tools.TextTyper;

namespace Marrow
{
	public class FeedTextTyper : MonoBehaviour
	{
		public TextTyper textTyper;

		public void UpdateText(string newText)
		{
			textTyper.TypeText(newText, 0.05f);
		}
	}
}
