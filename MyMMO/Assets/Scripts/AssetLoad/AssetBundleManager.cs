using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Tools;

public class AssetBundleManager : Singleton<AssetBundleManager>
{
    // *crc,资源块(所有资源) key为路径的Crc
    protected Dictionary<uint, ResourceItem> m_ResourceItemDic = new Dictionary<uint, ResourceItem>();

    // *crc,AbItem（包含ab包和引用计数）key为AB包名的Crc
    protected Dictionary<uint, AssetBundleItem> m_AssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();

    // *AB包资源块类对象池
    protected ClassObjectPool<AssetBundleItem> m_AssetBundleItemPool = ObjectPool.Instance.GetOrCreateClassPool<AssetBundleItem>(500);

    // *资源数据装箱
    public bool LoadAssetBundleConfig()
	{
#if UNITY_EDITOR

        if (!ResourceManager.Instance.m_LoadFromAssetBundle)
                return false;
#endif

        m_ResourceItemDic.Clear();
        AssetBundle assetBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/Assetbundleconfig");
        TextAsset textAsset = assetBundle.LoadAsset<TextAsset>("assetBundle.bytes");
        if (textAsset == null)
        {
            Debug.LogError($" {textAsset} 不存在！！ ");
            return false;
        }
        MemoryStream stream = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBundleConfig assetBundleConfig = (AssetBundleConfig)bf.Deserialize(stream);//--反序列化byte文件
        stream.Close();

        

        for (int i = 0; i < assetBundleConfig.AbList.Count; i++)
        {
            AbBase tempABBase = assetBundleConfig.AbList[i];
            ResourceItem resourceItem = new ResourceItem()
            {
                m_Crc = tempABBase.Crc,
                m_AssetName = tempABBase.AssetName,
                m_AssetBundleName = tempABBase.AbName,
                m_DependAssetBundle = tempABBase.AbDepend
            };
            
            
            if (m_ResourceItemDic.ContainsKey(resourceItem.m_Crc))
            {
                Debug.LogError($"重复的Crc 资源名：{resourceItem.m_AssetName} AB包名{resourceItem.m_AssetBundleName}");
            }
            else
            {
                m_ResourceItemDic.Add(resourceItem.m_Crc, resourceItem);
            }
        }

        Debug.Log("加载成功！" + m_ResourceItemDic.Count);
        foreach (KeyValuePair<uint, ResourceItem> kvp in m_ResourceItemDic)
        {
            Debug.Log($"Crc: {kvp.Key}   AB包名：{kvp.Value.m_AssetBundleName} 资源名称：{kvp.Value.m_AssetName}");
        }

        return true;

    }


    // *查找resourceItem
    public ResourceItem FindResourceItem(uint crc)
    {
        if(!m_ResourceItemDic.TryGetValue(crc,  out var resourceItem))
		{
            return null;
		}
		else
		{
            return resourceItem;

        }
        //return m_ResourceItemDic[crc];
    }

    /*------------------------------加载-------------------------*/

    // *加载ResourceItem
    public ResourceItem LoadResourceAssetBundle(uint crc)
    {
        ResourceItem item = null;

        if (!m_ResourceItemDic.TryGetValue(crc, out item) || item == null)//如果从字典里没取到值得话
        {
            Debug.LogError($"LoadResourceAssetBundle error: can not find crc{crc.ToString()} in AssetBundleConfig");
            return item;
        }
        if (item.m_AssetBundle != null)
        {
            return item;
        }
        item.m_AssetBundle = LoadAssetBundle(item.m_AssetBundleName);//加载该AB包并给item的AB包赋值

        if (item.m_DependAssetBundle != null)
        {
            for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
            {
                LoadAssetBundle(item.m_DependAssetBundle[i]);//把item得依赖AB包都给加载出来
            }
        }
        return item; //根据crc返回一个完整的resourceItem
    }

    // *加载单个assetBundle
    private AssetBundle LoadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = Crc32.GetCrc32(name);

        if (!m_AssetBundleItemDic.TryGetValue(crc, out item))
        {
            AssetBundle assetBundle = null;

            string fullPath = Application.streamingAssetsPath + "/" + name;
            if (File.Exists(fullPath))
            {
                assetBundle = AssetBundle.LoadFromFile(fullPath);
            }
            if (assetBundle == null)
            {
                Debug.LogError($"Load ab error: {fullPath}");
            }

            item = m_AssetBundleItemPool.Spawn(true);//从AssetBundleItemPool取值
            item.assetBundle = assetBundle;
            item.RefCount++;//引用计数++
            m_AssetBundleItemDic.Add(crc, item);
        }
        else
        {
            item.RefCount++;
        }

        return item.assetBundle;
    }

    /*------------------------------卸载-------------------------*/

    // *卸载资源
    public void ReleaseAsset(ResourceItem item)
    {
        if (item == null)
        {
            return;
        }
        if (item.m_DependAssetBundle != null && item.m_DependAssetBundle.Count > 0)
        {
            for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
            {
                UnLoadAssetBundle(item.m_DependAssetBundle[i]);//依赖AB包卸载
            }
        }
        UnLoadAssetBundle(item.m_AssetBundleName);//该AB包卸载

    }

    // *卸载单个assetBundle（卸载时会根据引用计数来判断是否真的从内存中卸载）
    private void UnLoadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = Crc32.GetCrc32(name);
        if (m_AssetBundleItemDic.TryGetValue(crc, out item) && item != null)
        {
            item.RefCount--;
            if (item.RefCount <= 0 && item.assetBundle != null)
            {
                item.assetBundle.Unload(true);
                item.Rest();
                m_AssetBundleItemPool.Recycle(item);
                m_AssetBundleItemDic.Remove(crc);
            }
		}
		else
		{
            //Debug.LogError($"m_AssetBundleItemDic 没有:{name} 此包");
		}
    }

}


#region 结构类
// *基础资源块
public class ResourceItem
{
    // *资源路径的crc
    public uint m_Crc = 0;

    // *资源的文件名
    public string m_AssetName = string.Empty;

    // *资源所在的AB名称
    public string m_AssetBundleName = string.Empty;

    // *资源所依赖的AB包
    public List<string> m_DependAssetBundle = null;

    // *该资源加载完的AB包
    public AssetBundle m_AssetBundle = null;
    
    // *资源对象
    public UnityEngine.Object m_Obj = null;

    // *资源的唯一ID
    public int m_Guid = 0;
    
    // *资源最后所使用的时间
    public float m_LastUseTime = 0.0f;
    
    // *引用计数
    public int m_RefCount = 0;

    // *是否跳场景清掉
    public bool m_Clear = true;

    public int RefCount
    {
        get { return m_RefCount; }
        set
        {
            m_RefCount = value;
            if (m_RefCount < 0)
            {
                Debug.LogError("refCount<0" + m_RefCount + "," + (m_Obj != null ? m_Obj.name : "name is null"));

            }
        }
    }
}

// *AB包资源块
public class AssetBundleItem
{
    public AssetBundle assetBundle = null;
    public int RefCount;//引用计数

    public void Rest()
    {
        assetBundle = null;
        RefCount = 0;
    }
}

#endregion