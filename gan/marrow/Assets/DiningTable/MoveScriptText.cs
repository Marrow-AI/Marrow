using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Marrow
{
	public class MoveScriptText : MonoBehaviour
	{

		public MoveSpotLight moveSpotLight;
		public Transform[] textPositions;
		private TextMeshPro textMeshProScriptText;

		private void Start()
		{
			textMeshProScriptText = GetComponent<TextMeshPro>();
		}

		public void OnReceivedOscMsg(extOSC.OSCMessage message)
		{
			//Debug.Log(message);
			UpdateScriptPosition(message.Values[0].StringValue);
			UpdateText(message.Values[1].StringValue);
		}

		void UpdateText(string newText)
		{
          Debug.Log("Update text: " + newText);
			textMeshProScriptText.text = newText;
		}

		public void UpdateScriptPosition(string role)
		{

			moveSpotLight.UpdateSpotlightPosition(role);

			//LeanTween.value(textMeshProScriptText.gameObject, Color.clear, Color.white, 1f)
			//.setOnUpdate(CallOnColorUpdate);

			int roleIndex = 0;
			switch (role)
			{
				case "dad":
					roleIndex = 0;
					break;
				case "sister":
					roleIndex = 1;
					break;
				case "brother":
					roleIndex = 2;
					break;
				case "mom":
					roleIndex = 3;
					break;
			}
			transform.position = textPositions[roleIndex].position;
			transform.rotation = textPositions[roleIndex].rotation;
		}

		private void CallOnColorUpdate(Color col)
		{
			textMeshProScriptText.color = col;
		}
	}
}
