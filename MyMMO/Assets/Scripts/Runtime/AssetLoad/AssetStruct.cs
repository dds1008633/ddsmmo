using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetStruct 
{
    
}
#region 链表
//双向链表节点
public class DoubleLinkedListNode<T> where T : class, new()
{
    //前一个结点
    public DoubleLinkedListNode<T> prev = null;

    //下一个节点
    public DoubleLinkedListNode<T> next = null;

    //当前节点
    public T t = null;
}

public class DoubleLinedList<T> where T : class, new()
{
    public DoubleLinkedListNode<T> Head = null;//表头

    public DoubleLinkedListNode<T> Tail = null;//表尾

    //双向链表结构类对象池
    protected ClassObjectPool<DoubleLinkedListNode<T>> m_DoubleLinkNodePool = ObjectPool.Instance.GetOrCreateClassPool<DoubleLinkedListNode<T>>(500);

    protected int m_Count = 0;

    public int Count
    {
        get { return m_Count; }

    }

    //添加头节点
    public DoubleLinkedListNode<T> AddToHeader(T t)
    {
        DoubleLinkedListNode<T> pNode = m_DoubleLinkNodePool.Spawn(true);
        pNode.next = null;
        pNode.prev = null;
        pNode.t = t;
        return (AddToHeader(pNode));
    }
    public DoubleLinkedListNode<T> AddToHeader(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;

        pNode.prev = null;
        if (Head == null)
        {
            Head = Tail = null;
        }
        else
        {
            pNode.next = Head;
            Head.prev = pNode;
            Head = pNode;
        }

        m_Count++;
        return Head;
    }
    //添加尾节点
    public DoubleLinkedListNode<T> AddToTail(T t)
    {
        DoubleLinkedListNode<T> pNode = m_DoubleLinkNodePool.Spawn(true);
        pNode.next = null;
        pNode.prev = null;
        pNode.t = t;
        return (AddToTail(pNode));
    }
    public DoubleLinkedListNode<T> AddToTail(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;
        pNode.next = null;
        if (Tail == null)
        {
            Head = Tail = null;
        }
        else
        {
            pNode.prev = Tail;
            Tail.next = pNode;
            Tail = pNode;
        }
        m_Count++;
        return Tail;
    }

    //移除节点
    public void RemoveNode(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return;
        if (pNode == Head)
        {
            Head = pNode.next;
        }
        if (pNode == Tail)
        {
            Tail = pNode.prev;
        }

        if (pNode.prev != null)
        {
            pNode.prev.next = pNode.next;
        }
        if (pNode.next != null)
        {
            pNode.next.prev = pNode.prev;
        }
        pNode.next = pNode.prev = null;
        pNode.t = null;
        m_DoubleLinkNodePool.Recycle(pNode);
        m_Count--;
    }

    //把某个节点移动到头部
    public void MoveToHead(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null || pNode == Head)
            return;

        if (pNode.prev == null && pNode.next == null) //这个节点有可能是刚回收的节点
            return;

        if (pNode == Tail)
            Tail = pNode.prev;

        if (pNode.prev != null)
            pNode.prev.next = pNode.next;
        if (pNode.next != null)
            pNode.next.prev = pNode.prev;

        pNode.prev = null;
        pNode.next = Head;
        Head.prev = pNode;
        Head = pNode;

        if (Tail == null)//如果只有两个节点时会有这种情况
        {
            Tail = Head;
        }
    }
}

public class CMapList<T> where T : class, new()
{
    DoubleLinedList<T> m_Dlink = new DoubleLinedList<T>();
    Dictionary<T, DoubleLinkedListNode<T>> m_FindMap = new Dictionary<T, DoubleLinkedListNode<T>>();


    ~CMapList()
    {
        Clear();
    }


    public void Clear()
    {
        while (m_Dlink.Tail != null)
        {
            Remove(m_Dlink.Tail.t);
        }
    }


    /// <summary>
    /// 插入一个节点到表头
    /// </summary>
    /// <param name="t"></param>
    public void InsertToHead(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (m_FindMap.TryGetValue(t, out node) && node != null)
        {
            m_Dlink.AddToHeader(node);
            return;
        }
        m_Dlink.AddToHeader(t);
        m_FindMap.Add(t, node);
    }

    /// <summary>
    /// 从表尾弹出一个节点
    /// </summary>
    public void Pop()
    {
        if (m_Dlink.Tail != null)
        {
            Remove(m_Dlink.Tail.t);
        }
    }

    /// <summary>
    /// 删除某个节点
    /// </summary>
    /// <param name="t"></param>
    public void Remove(T t)
    {
        if (!m_FindMap.TryGetValue(t, out var node) || node == null)
        {
            return;
        }
        m_Dlink.RemoveNode(node);
        m_FindMap.Remove(t);
    }

    /// <summary>
    /// 返回尾部节点
    /// </summary>
    /// <returns></returns>
    public T Trail()
    {
        return m_Dlink.Tail == null ? null : m_Dlink.Tail.t;
    }
    /// <summary>
    /// 返回节点个数
    /// </summary>
    /// <returns></returns>
    public int Size()
    {
        return m_FindMap.Count;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Find(T t)
    {
        if (!m_FindMap.TryGetValue(t, out var node) || node == null)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 刷新某个节点，把节点移动到头部
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public bool Reflesh(T t)
    {
        if (!m_FindMap.TryGetValue(t, out var node) || node == null)
            return false;

        m_Dlink.MoveToHead(node);
        return true;
    }
}
#endregion

#region 异步加载枚举
public enum LoadResPriority
{
    RES_HIGTH = 0,//最高优先级
    RES_MIDDLE,//一般优先级
    RES_SLOW,//低优先级
    RES_NUM,//数量
}
#endregion

#region 异步加载回调类
public class AsyncCallBack
{
    //--加载完成事件
    public ResourceManager.OnAsyncObjFinish m_DealFinish = null;

    public ResourceObj m_ResObj = null;

    //--实例化物体加载完成的回调
    public ResourceManager.OnAsyncGoObjFinish m_dealObjFinsh = null;
    //事件参数
    public object m_Param1 = null, m_Param2 = null, m_Parma3 = null;

    public void Reset()
    {
        m_DealFinish = null;
        m_dealObjFinsh = null;
        m_Param1 = null;
        m_Param2 = null;
        m_Parma3 = null;
        m_ResObj = null;
    }
}
#endregion

#region 异步加载中间类
public class AsyncLoadResParam
{
    public List<AsyncCallBack> m_CallBackList = new List<AsyncCallBack>();
    public uint m_Crc;
    public string m_Path;
    public LoadResPriority m_Priority = LoadResPriority.RES_SLOW;

    public void Reset()
    {
        m_CallBackList.Clear();
        m_Crc = 0;
        m_Path = "";
        m_Priority = LoadResPriority.RES_SLOW;
    }
}

#endregion

#region 实例化Obj中间类
public class ResourceObj
{
    // *路径对应的CRC
    public uint m_Crc = 0;
    // *存ResourceItem
    public ResourceItem m_RestItem = null;
    // *实例化出来的GameObject
    public GameObject m_CloneObj = null;
    // *是否跳场景清楚
    public bool m_bClear = true;
    // *储存GUid
    public long m_Guid = 0;
    // *是否已经放回对象池
    public bool m_Already = false;
    // *是否放到场景节点下面
    public bool m_SetSceneParent = false;
    // *实例化资源加载完成回调
    public ResourceManager.OnAsyncObjFinish m_DealFinish = null;
    // *异步参数
    public object m_param1, m_param2, m_param3 = null;

    //*离线数据
    public OfflineData m_OfflineData = null;
    public void Reset()
    {
        m_Crc = 0;
        m_CloneObj = null;
        m_bClear = true;
        m_Guid = 0;
        m_Already = false;
        m_SetSceneParent = false;
        m_DealFinish = null;
        m_param1 = m_param2 = m_param3 = null;
        m_OfflineData = null;
    }
}
#endregion

