using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIOfflineData : OfflineData
{
	public Vector2[] m_AnchorMax;
	public Vector2[] m_AnchorMin;
	public Vector2[] m_Pivot;
	public Vector2[] m_SizeDelta;
	public Vector3[] m_AnchoredPos;
	public ParticleSystem[] m_Particle;

	public Button[] m_Buttons;
	public Toggle[] m_Toggles;

	public Image[] m_image;
	public Color[] m_color;
	public bool[] m_RaycastTarget;

	public Text[] m_Text;


	public override void ResetProp()
	{
		int allPointCount = mAllPoint.Length;
		for (int i = 0; i < allPointCount; i++)
		{
			RectTransform tempTrs = mAllPoint[i] as RectTransform;
			if (tempTrs != null)
			{
				tempTrs.localPosition = mPos[i];
				tempTrs.localRotation = mRot[i];
				tempTrs.localScale = mScale[i];
				tempTrs.anchorMax = m_AnchorMax[i];
				tempTrs.anchorMin = m_AnchorMin[i];
				tempTrs.pivot = m_Pivot[i];
				tempTrs.sizeDelta = m_SizeDelta[i];
				tempTrs.anchoredPosition3D = m_AnchoredPos[i];
				if (mAllPointActive[i])
				{
					if (!tempTrs.gameObject.activeSelf)
					{
						tempTrs.gameObject.SetActive(true);
					}
				}
				else
				{
					if (tempTrs.gameObject.activeSelf)
					{
						tempTrs.gameObject.SetActive(false);
					}
				}
				if (tempTrs.childCount > mAllPointChildCount[i])
				{
					int childCount = tempTrs.childCount;
					for (int j = mAllPointChildCount[i]; j < childCount; j++)
					{
						GameObject tempObj = tempTrs.GetChild(j).gameObject;
						GameObject.Destroy(tempObj);
					}
				}

			}
		}
	}

	public override void BindData()
	{
		Transform[] allTrs = gameObject.GetComponentsInChildren<Transform>(true);
		int allTrsCount = allTrs.Length;
		for (int i = 0; i < allTrsCount; i++)
		{
			if(!(allTrs[i] is RectTransform))
			{
				allTrs[i].gameObject.AddComponent<RectTransform>();
			}
		}

		m_Buttons = transform.GetComponentsInChildren<Button>();
		m_Toggles = transform.GetComponentsInChildren<Toggle>();

		m_Text = transform.GetComponentsInChildren<Text>();
		m_image = transform.GetComponentsInChildren<Image>();
		m_color = new Color[m_image.Length + m_Text.Length];
		m_RaycastTarget = new bool[m_image.Length];

		for (int i = 0; i < m_image.Length; i++)
		{
			m_color[i] = m_image[i].color;
			m_RaycastTarget[i] = m_image[i].raycastTarget;
		}

		for (int i = 0; i < m_Text.Length; i++)
		{
			m_color[i + m_image.Length] = m_Text[i].color;
		}

		mAllPoint = gameObject.GetComponentsInChildren<RectTransform>(true);
		m_Particle = gameObject.GetComponentsInChildren<ParticleSystem>(true);
		int allPointCount = mAllPoint.Length;
		mAllPointChildCount = new int[allPointCount];
		mAllPointActive = new bool[allPointCount];
		mPos = new Vector3[allPointCount];
		mRot = new Quaternion[allPointCount];
		mScale = new Vector3[allPointCount];
		m_Pivot = new Vector2[allPointCount];
		m_AnchorMax = new Vector2[allPointCount];
		m_AnchorMin = new Vector2[allPointCount];
		m_SizeDelta = new Vector2[allPointCount];
		m_AnchoredPos = new Vector3[allPointCount];
		for (int i = 0; i < allPointCount; i++)
		{
			RectTransform temp = mAllPoint[i] as RectTransform;
			mAllPointChildCount[i] = temp.childCount;
			mAllPointActive[i] = temp.gameObject.activeSelf;
			mPos[i] = temp.localPosition;
			mRot[i] = temp.localRotation;
			mScale[i] = temp.localScale;
			m_Pivot[i] = temp.pivot;
			m_AnchorMax[i] = temp.anchorMax;
			m_AnchorMin[i] = temp.anchorMin;
			m_SizeDelta[i] = temp.sizeDelta;
			m_AnchoredPos[i] = temp.anchoredPosition3D;
		}


	}
}
