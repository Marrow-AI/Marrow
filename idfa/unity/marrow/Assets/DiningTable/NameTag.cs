using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Marrow
{
	public class NameTag : MonoBehaviour
	{
		private Renderer m_renderer;
		private Vector3 originalPosition;
		private Vector3 showPosition;
		private bool m_isOn;
		public bool IsOn { get { return m_isOn; }}

		void Start()
		{
			m_renderer = GetComponent<Renderer>();
			originalPosition = transform.position;
			showPosition = transform.position + transform.up * 1.5f;
		}

		public void Show()
		{
			if (m_isOn) return;
			LeanTween.move(gameObject, showPosition, 1f);
			m_isOn = true;
		}

		public void Hide()
		{
			if (!m_isOn) return;
			LeanTween.move(gameObject, originalPosition, 1f);
			m_isOn = false;
		}
	}
}
