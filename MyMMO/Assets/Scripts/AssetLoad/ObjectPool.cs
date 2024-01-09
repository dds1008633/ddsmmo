using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TCMAsset.TCM_CSScripts.Tools;
using System;

public class ObjectPool:Singleton<ObjectPool>
{
    // *类对象池结构方法 (Type,ClassObjectPool<T>)
    protected Dictionary<Type, object> m_ClassPoolDic = new Dictionary<Type, object>();//类名，类池

    // *取
    public ClassObjectPool<T> GetOrCreateClassPool<T>(int maxCount) where T : class, new()
    {
        Type type = typeof(T);
        object outObj = null;

        if (!m_ClassPoolDic.TryGetValue(type, out outObj) || outObj == null)
        {
            ClassObjectPool<T> newPool = new ClassObjectPool<T>(maxCount);
            m_ClassPoolDic.Add(type, newPool);
            return newPool;
        }

        return outObj as ClassObjectPool<T>;

    }
}

public class ClassObjectPool<T> where T : class, new()
{
    // *池
    protected Stack<T> m_Pool = new Stack<T>();

    // *最大对象个数，<=0表示不限个数
    protected int m_MaxCount = 0;

    // *没有回收的对象个数
    protected int m_NoRecycleCount = 0;

    // *存
    public ClassObjectPool(int maxCount)
    {
        m_MaxCount = maxCount;
        for (int i = 0; i < maxCount; i++)
        {
            m_Pool.Push(new T());
        }
    }

    /// <summary>
    /// 从池里取类对象
    /// </summary>
    /// <param name="creatIfPoolEmpty">如果为空是否new出来</param>
    /// <returns></returns>
    public T Spawn(bool creatIfPoolEmpty)
    {
        if (m_Pool.Count > 0)//如果池子里有值
        {
            T rtn = m_Pool.Pop();
            if (rtn == null)
            {
                if (creatIfPoolEmpty)
                {
                    rtn = new T();
                }
            }

            m_NoRecycleCount++;//没有回收的对象个数+1
            return rtn;
        }
        else//如果池子是空的
        {
            if (creatIfPoolEmpty)
            {
                T rtn = new T();
                m_NoRecycleCount++;//没有回收的对象个数+1
                return rtn;
            }
        }
        return null;
    }

    /// <summary>
    /// 回收类对象
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool Recycle(T obj)
    {
        if (obj == null)
            return false;

        m_NoRecycleCount--;
        if (m_Pool.Count >= m_MaxCount && m_MaxCount > 0)
        {
            obj = null;
            return false;
        }

        m_Pool.Push(obj);
        return true;

    }


}
