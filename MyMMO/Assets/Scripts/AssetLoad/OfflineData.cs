using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfflineData : MonoBehaviour
{
    // *刚体
    public Rigidbody mRigidbody;

    // *碰撞器
    public Collider2D mCollider;

    // *所有节点
    public Transform[] mAllPoint;

    // *子物体个数
    public int[] mAllPointChildCount;

    // *所有的显示、隐藏控制
    public bool[] mAllPointActive;

    // *自身及子物体的位置信息
    public Vector3[] mPos;

    // *自身及子物体的缩放信息
    public Vector3[] mScale;

    // *自身及子物体的旋转信息
    public Quaternion[] mRot;


    /// <summary>
    /// 还原属性 
    /// 
    /// </summary>
    public virtual void ResetProp()
	{
        var allPointCount = mAllPoint.Length;
		for (int i = 0; i < allPointCount; i++)
		{
            var tempTrs = mAllPoint[i];
            if (!tempTrs) return;
            tempTrs.localPosition = mPos[i];
            tempTrs.localRotation = mRot[i];
            tempTrs.localScale = mScale[i];

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

            //TODO
            
		}
	}
    /// <summary>
    /// 编辑器下保存初始数据
    /// </summary>
    public virtual void BindData()
	{
        mCollider = gameObject.GetComponentInChildren<Collider2D>(true);
        mRigidbody = gameObject.GetComponentInChildren<Rigidbody>(true);
        mAllPoint = gameObject.GetComponentsInChildren<Transform>(true);
        var allPointCount = mAllPoint.Length;
        mAllPointChildCount = new int[allPointCount];
        mAllPointActive = new bool[allPointCount];
        mPos = new Vector3[allPointCount];
        mRot = new Quaternion[allPointCount];
        mScale = new Vector3[allPointCount];
		for (int i = 0; i < allPointCount; i++)
		{
            var temp = mAllPoint[i];
            mAllPointChildCount[i] = temp.childCount;
            mAllPointActive[i] = temp.gameObject.activeSelf;
            mPos[i] = temp.localPosition;
            mRot[i] = temp.localRotation;
            mScale[i] = temp.localScale;
        }
	}
}
