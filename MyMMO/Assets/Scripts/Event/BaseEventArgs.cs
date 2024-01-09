using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �¼�����
/// </summary>
public class BaseEventArgs : EventArgs, IReference
{

    protected int id;
    /// <summary>
    /// ��ȡ���ͱ��
    /// </summary>
    public int Id
    {
        get => id;
        set => id = value;
    }

    public void Clear()
    {

    }

}

public class TestEventArgs : BaseEventArgs
{

    public string aaaa;

    public static TestEventArgs Create(int id,string aaa)
    {
        TestEventArgs args = ReferencePool.Acquire<TestEventArgs>();
        args.aaaa = aaa;
        args.Id = id;
        return args;
    }
}
