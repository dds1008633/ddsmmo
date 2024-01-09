using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TCMAsset.TCM_CSScripts;
using Tools;

public class ResourceManager : Singleton<ResourceManager>
{
    protected long guid = 0;

    // *Mono脚本
    protected MonoBehaviour m_Startmono;
    // *是否从AB包里加载
    public bool m_LoadFromAssetBundle = false;

    // *资源缓存池(缓存加载过的所有资源列表)
    public Dictionary<uint, ResourceItem> AssetDic { get; set; } = new Dictionary<uint, ResourceItem>();

    // *缓存引用计数为零的资源列表，达到缓存最大的时候释放这个列表里面最早没用的资源
    protected CMapList<ResourceItem> m_NoRefrenceAssetMapList = new CMapList<ResourceItem>();

    // *Obj加载完成后事件
    public delegate void OnAsyncObjFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null);

    // *正在异步加载的资源列表（3个List）
    protected List<AsyncLoadResParam>[] m_LoadingAssetList = new List<AsyncLoadResParam>[(int)LoadResPriority.RES_NUM];
    
    // *正在异步加载的Dic
    protected Dictionary<uint, AsyncLoadResParam> m_LoadingAssetDic = new Dictionary<uint, AsyncLoadResParam>();

    // *异步加载的中间类，回调类的类对象池
    protected ClassObjectPool<AsyncLoadResParam> m_AsyncLoadResParamPool = ObjectPool.Instance.GetOrCreateClassPool<AsyncLoadResParam>(50);
    protected ClassObjectPool<AsyncCallBack> m_AsyncCallBackPool = ObjectPool.Instance.GetOrCreateClassPool<AsyncCallBack>(50);  
    
    // *最长连续卡着加载资源的时间，微秒单位
    private const long MAXLOADRESTIME = 20000;

    //链表最大缓存个数
    private const long MAXCACHECOUNT = 500;
    /*----------------------------------实例化Obj↓---------------------------- */

    // *ResourceObj的类的类对象池
    protected ClassObjectPool<ResourceObj> m_ResourceObjClassPool = ObjectPool.Instance.GetOrCreateClassPool<ResourceObj>(1000);

    // *对象池节点
    public Transform RecyclePoolTrs;

    // *场景节点
    public Transform SceneTrs;

    // *对象池
    protected Dictionary<uint, List<ResourceObj>> m_ObjectPoolDic = new Dictionary<uint, List<ResourceObj>>();

    // *暂存ResObj的Dic key值为GUID
    protected Dictionary<int, ResourceObj> m_ResourceObjDic = new Dictionary<int, ResourceObj>();

    // *根据异步的guid的储存ResourceObj来判断是否正在异步加载
    protected Dictionary<long, ResourceObj> m_AsyncResObjs = new Dictionary<long, ResourceObj>();//key guid

    // *实例化对象加载完成回调
    public delegate void OnAsyncGoObjFinish(string path, ResourceObj resObj, object param1 = null, object param2 = null, object param3 = null);

    /*-----------------------------------实例化Obj↑-----------------------------*/

    // *初始化
    public void Init(MonoBehaviour mono, Transform recycleTrs, Transform sceneTrs)
	{
        
        for (int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
        {
            m_LoadingAssetList[i] = new List<AsyncLoadResParam>();
        }
        m_Startmono = mono;
        m_Startmono.StartCoroutine(AsyncLoadCor());//开启异步加载协程
        RecyclePoolTrs = recycleTrs;
        SceneTrs = sceneTrs;
#if UNITY_EDITOR
        RecyclePoolTrs.gameObject.name = "对象池节点";
        SceneTrs.gameObject.name = "场景节点";
#endif
    }

    // *增加GUID
    public long CreateGuid()
    {
        return guid++;
    }

    #region (不需要实例化的资源)

    /*-------------------------------------不需要实例化的资源-------------------------------*/

    // *同步加载(不需要实例化的)
    public T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        uint crc = Crc32.GetCrc32(path);
        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            return item.m_Obj as T;
        }
        T obj = null;
		if (GameConfig.loadType == LoadType.AssetDataBase || GameConfig.loadType == LoadType.Resources)
		{
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as T;
                }
                else
                {

                    obj = LoadAssetByEditor<T>(path);
                }

            }
            else
            {
                item = new ResourceItem();
                item.m_Crc = crc;
                obj = LoadAssetByEditor<T>(path);
            }
        }
		else
		{
            if (obj == null)
            {
                item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
                if (item != null && item.m_AssetBundle != null)
                {
                    if (item.m_Obj != null)
                    {
                        obj = item.m_Obj as T;
                    }
                    else
                    {
                        obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
                    }

                }
            }
        }
         
        CacheResource(path, ref item, crc, obj);
        return obj;
    }

    // *预加载(不需要实例化的)
    public void PreloadRes(string path)
	{
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        uint crc = Crc32.GetCrc32(path);
        ResourceItem item = GetCacheResourceItem(crc, 0);
        if (item != null)
        {
            return;
        }
        Object obj = null;
	
		if (GameConfig.loadType == LoadType.AssetDataBase || GameConfig.loadType == LoadType.Resources)
		{
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as Object;
                }
                else
                {
                    obj = LoadAssetByEditor<Object>(path);
                }

            }
            else
            {
                item = new ResourceItem();
                item.m_Crc = crc;
                obj = LoadAssetByEditor<Object>(path);
            }
		}
		else
		{
            if (obj == null)
            {
                item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
                if (item != null && item.m_AssetBundle != null)
                {
                    if (item.m_Obj != null)
                    {
                        obj = item.m_Obj;
                    }
                    else
                    {
                        obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                    }

                }
            }
        }    
    
        CacheResource(path, ref item, crc, obj);
        item.m_Clear = false;
        ReleaseResource(path);
    }

    // *异步加载(不需要实例化的)
    public void AsyncLoadResource(string path, OnAsyncObjFinish dealFinish, LoadResPriority priority, object param1 = null,
        object param2 = null, object param3 = null, uint crc = 0)
	{
        if (crc == 0)
        {
            crc = Crc32.GetCrc32(path);
        }
        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            if (dealFinish != null)
            {
                dealFinish(path, item.m_Obj, param1, param2, param3);
            }
            return;
        }

        // --判断是否加载中
        AsyncLoadResParam para = null;
        if (!m_LoadingAssetDic.TryGetValue(crc, out para) || para == null)
		{
            para = m_AsyncLoadResParamPool.Spawn(true);
            para.m_Crc = crc;
            para.m_Path = path;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(crc, para);
            m_LoadingAssetList[(int)priority].Add(para);
		}
        //--往回调列表里面加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
        callBack.m_DealFinish = dealFinish;
        callBack.m_Param1 = param1;
        callBack.m_Param2 = param2;
        callBack.m_Parma3 = param3;
        para.m_CallBackList.Add(callBack);

    }


    // *编辑器加载
    protected T LoadAssetByEditor<T>(string path) where T : UnityEngine.Object
    {
		switch (GameConfig.loadType)
		{
			case LoadType.Resources:
				return ResourcesLoadByEditor<T>(path);

#if UNITY_EDITOR
			case LoadType.AssetDataBase:
				return AssetDatabase.LoadAssetAtPath<T>(path);
#endif
		}
		Debug.LogError("加载方式出错!");
        return null;
    }

    // *Resources加载
    private string loadPathHear = "Assets/TCMAsset/Resources/";
    protected T ResourcesLoadByEditor<T>(string path) where T : UnityEngine.Object
	{
        var idx = path.LastIndexOf('.');
        string newPath = path.Substring(0, idx);
        newPath= newPath.Replace(loadPathHear, "");
        var obj= Resources.Load<T>(newPath);
        return obj;
	}
    

    // *资源卸载(根据路径)（不需要实例化的资源）
    public bool ReleaseResource(string path, bool destoryObj = false)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        uint crc = Crc32.GetCrc32(path);
        ResourceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
        {
            Debug.LogError($"AssetDic里不存在该资源 {path} 可能释放了多次");
        }

        item.RefCount--;
        DestoryResourceItem(item, destoryObj);
        return true;

    }

    // *资源卸载(根据对象)(不需要实例化的资源)
    public bool ReleaseResource(Object obj, bool destoryObj = false)
    {
        if (obj == null)
            return false;
        ResourceItem item = null;
        foreach (ResourceItem res in AssetDic.Values)
        {
            if (res.m_Guid == obj.GetInstanceID())
            {
                item = res;
            }
        }
        if (item == null)
        {
            Debug.LogError($"AssetDic里不存在该资源 {obj.name} 可能释放了多次");
        }
        item.RefCount--;
        DestoryResourceItem(item, destoryObj);
        return true;
    }

    // *回收ResourceItem
    protected void DestoryResourceItem(ResourceItem item, bool destoryCache = false)
    {
        if (item == null || item.RefCount > 0) //--引用计数大于0则不清除
            return;

        if (!destoryCache) //--不清除缓存
        {
            m_NoRefrenceAssetMapList.InsertToHead(item);
            return;
        }
        if (!AssetDic.Remove(item.m_Crc))
        {
            return;
        }
        m_NoRefrenceAssetMapList.Remove(item);

        //--释放assetbundle引用
        AssetBundleManager.Instance.ReleaseAsset(item);

        //--清空资源对应的对象池
        ClearPoolObject(item.m_Crc);

        if (item.m_Obj != null)
        {
            item.m_Obj = null;
        }

        //-编辑器内卸载
        Resources.UnloadUnusedAssets();


    }

    // *异步加载协程
    IEnumerator AsyncLoadCor()
	{
        List<AsyncCallBack> callBackList = new List<AsyncCallBack>();
        long lastYiledTime = System.DateTime.Now.Ticks; //微秒

		while (true)
		{
            bool haveYield = false;
			for (int i = 0; i < (int)LoadResPriority.RES_NUM; i++) //--从最高级开始
			{
                List<AsyncLoadResParam> loadingList = m_LoadingAssetList[i];
                if (loadingList.Count <= 0)
                    continue;

                AsyncLoadResParam loadingItem = loadingList[0];
                loadingList.RemoveAt(0);
                callBackList = loadingItem.m_CallBackList;
                Object obj = null;
                ResourceItem item = null;
	
				if (GameConfig.loadType == LoadType.AssetDataBase || GameConfig.loadType == LoadType.Resources)
				{
                    item = AssetBundleManager.Instance.FindResourceItem(loadingItem.m_Crc);
                    obj = LoadAssetByEditor<Object>(loadingItem.m_Path);
                    if (item == null)
                    {
                        item = new ResourceItem();
                        item.m_Crc = loadingItem.m_Crc;
                    }

                    yield return new WaitForSeconds(0.5f);
                }
				else
				{
                    if (obj == null)
                    {
                        item = AssetBundleManager.Instance.LoadResourceAssetBundle(loadingItem.m_Crc);
                        if (item != null && item.m_AssetBundle != null)
                        {
                            AssetBundleRequest abRequest = item.m_AssetBundle.LoadAssetAsync<Object>(item.m_AssetName);
                            yield return abRequest;
                            if (abRequest.isDone)
                            {
                                obj = abRequest.asset;

                            }
                            lastYiledTime = System.DateTime.Now.Ticks;
                        }
                    }
                }
               

				
                CacheResource(loadingItem.m_Path, ref item, loadingItem.m_Crc, obj, callBackList.Count);

				for (int j = 0; j < callBackList.Count; j++)
				{
                    AsyncCallBack callBack = callBackList[j];

					if (callBack != null && callBack.m_DealFinish != null) //--无需实例化的回调执行
					{
                        callBack.m_DealFinish(loadingItem.m_Path, obj, callBack.m_Param1, callBack.m_Param2, callBack.m_Parma3);

					}

                    if (callBack != null && callBack.m_dealObjFinsh != null && callBack.m_ResObj != null)//--需实例化的回调执行
                    {
                        ResourceObj tempResObj = callBack.m_ResObj;
                        tempResObj.m_RestItem = item;
                        callBack.m_dealObjFinsh(loadingItem.m_Path, tempResObj, tempResObj.m_param1, tempResObj.m_param2, tempResObj.m_param3);
                        callBack.m_dealObjFinsh = null;
                        tempResObj = null;
                    }

                    callBack.Reset();
                    m_AsyncCallBackPool.Recycle(callBack);
                }
                obj = null;
                callBackList.Clear();
                m_LoadingAssetDic.Remove(loadingItem.m_Crc);
                loadingItem.Reset();
                m_AsyncLoadResParamPool.Recycle(loadingItem);

                if (System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
                {
                    yield return null;//等待一帧  yield return 后面如果跟数字的话，不论是0 1 2还是什么 都是等待一帧
                    lastYiledTime = System.DateTime.Now.Ticks;
                    haveYield = true;
                }


                

            }
            if (!haveYield || System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
            {
                lastYiledTime = System.DateTime.Now.Ticks;
                yield return null;//等待一帧  yield return 后面如果跟数字的话，不论是0 1 2还是什么 都是等待一帧
            }
        }
    }


    #endregion

    #region (需要实例化的资源)
    /*-------------------------------------需要实例化的资源-------------------------------*/

    // *同步加载(需要实例化的)
    public GameObject LoadInstantiateResource(string path,Transform parent=null, bool setSceneObj = false, bool bClear = true)
    {
        uint crc = Crc32.GetCrc32(path);
        ResourceObj resourceObj = GetObjectFromPool(crc);
        if (resourceObj == null)
        {
            resourceObj = m_ResourceObjClassPool.Spawn(true);
            resourceObj.m_Crc = crc;
            resourceObj.m_bClear = bClear;
            resourceObj.m_SetSceneParent = setSceneObj;

            //resouceitem在此时赋值给 resourceObj
            resourceObj = LoadResource(path, resourceObj);

            if (resourceObj.m_RestItem.m_Obj != null)
            {
                resourceObj.m_CloneObj = GameObject.Instantiate(resourceObj.m_RestItem.m_Obj) as GameObject;
                resourceObj.m_OfflineData = resourceObj.m_CloneObj.GetComponent<OfflineData>();
            }
        }
		if (parent)
		{
            resourceObj.m_CloneObj.transform.SetParent(parent, false);
		}
		else
		{
            resourceObj.m_CloneObj.transform.parent = null;
        }
	
        if (setSceneObj)
        {
            resourceObj.m_CloneObj.transform.SetParent(SceneTrs, false);
        }
        int tempID = resourceObj.m_CloneObj.GetInstanceID();
        if (!m_ResourceObjDic.ContainsKey(tempID))
        {
            m_ResourceObjDic.Add(tempID, resourceObj);
        }
        return resourceObj.m_CloneObj;
    }

    // *预加载(需要实例化的)
    public void PreloadInstantiateRes(string path,Transform parent=null, int count = 1, bool clear = false)
    {
        List<GameObject> tempGameObjectList = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject obj = LoadInstantiateResource(path, parent,false, bClear: clear);
            tempGameObjectList.Add(obj);
        }
		for (int i = 0; i < count; i++)
		{
            GameObject obj = tempGameObjectList[i];
            ReleaseObject(obj);// --回收到对象池
            obj = null;
        }
        tempGameObjectList.Clear();
    }

    // *异步加载(需要实例化的)
    public long AsyncInstantiateObject(string path, OnAsyncObjFinish dealFinish, LoadResPriority priority, bool setSceneObject = false, object param1 = null,
       object param2 = null, object param3 = null, bool bClear = true)
	{
        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }
        uint crc = Crc32.GetCrc32(path);
        ResourceObj resObj = GetObjectFromPool(crc);
        if (resObj != null)
        {
            if (setSceneObject)
            {
                resObj.m_CloneObj.transform.SetParent(SceneTrs, false);
            }

            if (dealFinish != null)
            {
                dealFinish(path, resObj.m_CloneObj, param1, param2, param3);
            }

            return resObj.m_Guid;
        }
        long guid = CreateGuid();
        resObj = m_ResourceObjClassPool.Spawn(true);
        resObj.m_Crc = crc;
        resObj.m_SetSceneParent = setSceneObject;
        resObj.m_bClear = bClear;
        resObj.m_DealFinish = dealFinish;
        resObj.m_param1 = param1;
        resObj.m_param2 = param2;
        resObj.m_param3 = param3;
        resObj.m_Guid = guid;
        m_AsyncResObjs.Add(guid, resObj);
        AsyncLoadResource(path, resObj, OnLoadResourceObjFinish, priority);
        return guid;
    }

    // *取消异步加载(需要实例化的)
    public void CancelLoad(long guid)
    {
        ResourceObj resourceObj = null;
        if (m_AsyncResObjs.TryGetValue(guid, out resourceObj) && CancelLoad(resourceObj))
        {
            m_AsyncResObjs.Remove(guid);
            resourceObj.Reset();
            m_ResourceObjClassPool.Recycle(resourceObj);
            return;
        }

        Debug.LogError("不能取消异步加载！！！");
    }
    // *取消异步加载
    public bool CancelLoad(ResourceObj res)
    {
        AsyncLoadResParam para = null;
        if (m_LoadingAssetDic.TryGetValue(res.m_Crc, out para) && m_LoadingAssetList[(int)para.m_Priority].Contains(para))
        {
            for (int i = para.m_CallBackList.Count; i >= 0; i--)
            {
                AsyncCallBack tempCallBack = para.m_CallBackList[i];
                if (tempCallBack != null && res == tempCallBack.m_ResObj)
                {
                    tempCallBack.Reset();
                    m_AsyncCallBackPool.Recycle(tempCallBack);
                    para.m_CallBackList.Remove(tempCallBack);
                }
            }

            if (para.m_CallBackList.Count <= 0)
            {
                para.Reset();
                m_LoadingAssetList[(int)para.m_Priority].Remove(para);
                m_AsyncLoadResParamPool.Recycle(para);
                m_LoadingAssetDic.Remove(res.m_Crc);
                return true;
            }

        }

        return false;

    }

    // *判断是否正在异步加载
    public bool IsingAsyncLoad(long guid)
    {
        return m_AsyncResObjs[guid] == null;
    }

    // *异步资源加载完成回调
    void OnLoadResourceObjFinish(string path, ResourceObj resObj, object param1 = null, object param2 = null, object param3 = null)
    {
        if (resObj == null)
            return;

        if (resObj.m_RestItem.m_Obj == null)
        {
#if UNITY_EDITOR
            Debug.LogError("异步加载资源为空:" + path);
#endif
        }
        else
        {
            resObj.m_CloneObj = GameObject.Instantiate(resObj.m_RestItem.m_Obj) as GameObject;
            resObj.m_OfflineData = resObj.m_CloneObj.GetComponent<OfflineData>();
        }

        //加载完成后把正在加载的dic中移除
        if (m_AsyncResObjs.ContainsKey(resObj.m_Guid))
        {
            m_AsyncResObjs.Remove(resObj.m_Guid);
        }

        if (resObj.m_CloneObj != null && resObj.m_SetSceneParent)
        {
            resObj.m_CloneObj.transform.SetParent(SceneTrs, false);
        }

        if (resObj.m_DealFinish != null)
        {
            int tempID = resObj.m_CloneObj.GetInstanceID();
            if (!m_ResourceObjDic.ContainsKey(tempID))
            {
                m_ResourceObjDic.Add(tempID, resObj);
            }

            resObj.m_DealFinish(path, resObj.m_CloneObj, resObj.m_param1, resObj.m_param2, resObj.m_param3);
        }


    }

    // *异步加载封装 异步加载中间类
    protected void AsyncLoadResource(string path, ResourceObj resObj, OnAsyncGoObjFinish dealFinish, LoadResPriority priority)
	{
        ResourceItem item = GetCacheResourceItem(resObj.m_Crc);
        if (item != null)
        {
            if (dealFinish != null)
            {
                dealFinish(path, resObj);
            }
            return;
        }
        AsyncLoadResParam para = null;
        if (!m_LoadingAssetDic.TryGetValue(resObj.m_Crc, out para) || para == null)
        {
            para = m_AsyncLoadResParamPool.Spawn(true);
            para.m_Crc = resObj.m_Crc;
            para.m_Path = path;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(resObj.m_Crc, para);
            m_LoadingAssetList[(int)priority].Add(para);
        }
        //往回调列表里面加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
        callBack.m_dealObjFinsh = dealFinish;
        callBack.m_ResObj = resObj;
        para.m_CallBackList.Add(callBack);
    }

    // *为ResObj提供ResourceItem
    protected ResourceObj LoadResource(string path, ResourceObj resObj)
    {
        if (resObj == null)
        {
            return null;
        }
        uint crc = resObj.m_Crc == 0 ? Crc32.GetCrc32(path) : resObj.m_Crc;
        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            resObj.m_RestItem = item;
            return resObj;
        }
        Object obj = null;
	
		if (GameConfig.loadType == LoadType.AssetDataBase || GameConfig.loadType == LoadType.Resources)
		{
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item == null)
            {
                item = new ResourceItem();
                item.m_Crc = crc;
            }

            if (item.m_Obj != null)
            {
                obj = item.m_Obj;
            }
            else
            {
                obj = LoadAssetByEditor<Object>(path);
            }
        }
		else
		{
            if (obj == null)
            {
                item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
                if (item != null && item.m_AssetBundle != null)
                {
                    if (item.m_Obj != null)
                    {
                        obj = item.m_Obj;
                    }
                    else
                    {
                        obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                    }

                }
            }
        }


        CacheResource(path, ref item, crc, obj);
        resObj.m_RestItem = item;
        item.m_Clear = resObj.m_bClear;

        return resObj;
    }

    // *资源卸载(需要实例化的资源)
    public bool ReleaseResource(ResourceObj resObj, bool destoryObj = false)
	{
        if (resObj == null)
        {
            return false;
        }
        if (!AssetDic.TryGetValue(resObj.m_Crc, out var item) || item == null)
        {
            Debug.LogError($"AssetDic里不存在该资源：" + resObj.m_CloneObj.name + " 可能释放了多次");
        }
        GameObject.Destroy(resObj.m_CloneObj);
        item.RefCount--;
        DestoryResourceItem(item, destoryObj);
        return true;
    }

    // *根据ResObj增加引用计数
    public int IncreaseResourceRef(ResourceObj resObj, int count = 1)
    {
        return resObj != null ? IncreaseResourceRef(resObj.m_Crc, count) : 0;
    }

    // *根据ResObj的Path增加引用计数
    public int IncreaseResourceRef(uint crc = 0, int count = 1)
    {
        ResourceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
            return 0;

        item.RefCount += count;
        item.m_LastUseTime = Time.realtimeSinceStartup;

        return item.RefCount;
    }

    // *根据ResObj减少引用计数
    public int DecreaseResourceRef(ResourceObj resObj, int count = 1)
    {
        return resObj != null ? DecreaseResourceRef(resObj.m_Crc, count) : 0;
    }

    // *根据ResObj的Path减少引用计数
    public int DecreaseResourceRef(uint crc = 0, int count = 1)
    {
        ResourceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
            return 0;

        item.RefCount -= count;

        return item.RefCount;
    }
    #endregion

    #region 对象池机制

    // *从对象池中取
    protected ResourceObj GetObjectFromPool(uint crc)
    {
        List<ResourceObj> st = null;
        if (m_ObjectPoolDic.TryGetValue(crc, out st) && st != null && st.Count > 0)
        {
            IncreaseResourceRef(crc);
            ResourceObj resObj = st[0];
            st.RemoveAt(0);
            GameObject obj = resObj.m_CloneObj;
            if (!System.Object.ReferenceEquals(obj, null))
            {
                if (!ReferenceEquals(resObj.m_OfflineData, null))
                {
                    resObj.m_OfflineData.ResetProp();//重置离线数据属性
                }
                resObj.m_Already = false;//标记为从已对象池里取出
#if UNITY_EDITOR

                if (obj.name.EndsWith("(Recycle)"))
                {
                    obj.name = obj.name.Replace("(Recycle)", "");
                }

#endif
            }
            return resObj;
        }
        return null;
    }
    // *资源回收（存入对象池）(需要实例化的资源)
    public void ReleaseObject(GameObject obj, int maxCacheCount =0, bool destoryCahe = false, bool recycleParent = true)
    {
        if (obj == null)
        {
            return;
        }
        ResourceObj resObj = null;
        int tempID = obj.GetInstanceID();

        if (!m_ResourceObjDic.TryGetValue(tempID, out resObj))
        {
            Debug.Log($"{obj.name} 对象不是ObjManager创建的！");
            return;
        }

        if (resObj == null)
        {
            Debug.LogError("缓存的ResourceObj为空");
            return;
        }

        if (resObj.m_Already)
        {
            Debug.LogError("该对象已经放回对象池");
            return;
        }
#if UNITY_EDITOR
        if (!obj.name.EndsWith("(Recycle)"))
        {
            obj.name += "(Recycle)";
        }

#endif
        if (maxCacheCount == 0) //--清除
        {
            m_ResourceObjDic.Remove(tempID);
            ReleaseResource(resObj, destoryCahe);
            resObj.Reset();
            m_ResourceObjClassPool.Recycle(resObj);
        }
        else //-回收到对象池
        {
            if (!m_ObjectPoolDic.TryGetValue(resObj.m_Crc, out var st) || st == null)
            {
                st = new List<ResourceObj>();
                m_ObjectPoolDic.Add(resObj.m_Crc, st);
            }
            if (resObj.m_CloneObj)
            {
                if (recycleParent)
                {
                    resObj.m_CloneObj.transform.SetParent(RecyclePoolTrs);
                }
                else
                {
                    resObj.m_CloneObj.SetActive(false);
                }
            }
            if (maxCacheCount < 0 || st.Count < maxCacheCount) //--放到池子里
            {
                st.Add(resObj);
                resObj.m_Already = true;
                DecreaseResourceRef(resObj);
            }
            else//多出来的 --清除
            {
                m_ResourceObjDic.Remove(tempID);
                ReleaseResource(resObj, destoryCahe);
                resObj.Reset();
                m_ResourceObjClassPool.Recycle(resObj);
            }

        }

    }

    // *是否由对象池创建的
    public bool IsObjectManagerCreat(GameObject obj)
    {
        ResourceObj resObj = m_ResourceObjDic[obj.GetInstanceID()];

        return resObj == null ? false : true;
    }
    
    // *清空对象池
    public void ClearPool()
    {
        List<uint> tempList = new List<uint>();
        foreach (uint key in m_ObjectPoolDic.Keys)
        {
            List<ResourceObj> st = m_ObjectPoolDic[key];
            st.ForEach(resObj =>
            {
                if (!ReferenceEquals(resObj.m_CloneObj, null) && resObj.m_bClear)
                {
                    GameObject.Destroy(resObj.m_CloneObj);
                    m_ResourceObjDic.Remove(resObj.m_CloneObj.GetInstanceID());
                    resObj.Reset();
                    m_ResourceObjClassPool.Recycle(resObj);
                }
            });

            if (st.Count <= 0)
            {
                tempList.Add(key);
            }
        }

        tempList.ForEach(temp =>
        {
            if (m_ObjectPoolDic.ContainsKey(temp))
            {
                m_ObjectPoolDic.Remove(temp);
            }
        });

        tempList.Clear();
    }

    // *清空某个资源在对象池中所有得对象
    public void ClearPoolObject(uint crc)
    {
        List<ResourceObj> tempList = new List<ResourceObj>();
        if (!m_ObjectPoolDic.TryGetValue(crc, out tempList) || tempList == null)
            return;


        for (int i = tempList.Count - 1; i >= 0; i--)
        {
            ResourceObj resObj = tempList[i];
            if (resObj.m_bClear)
            {
                tempList.Remove(resObj);
                int tempID = resObj.m_CloneObj.GetInstanceID();
                GameObject.Destroy(resObj.m_CloneObj);
                resObj.Reset();
                m_ResourceObjDic.Remove(tempID);
                m_ResourceObjClassPool.Recycle(resObj);
            }

        }

        if (tempList.Count <= 0)
            m_ObjectPoolDic.Remove(crc);
    }


    #endregion

    #region 缓存池机制

    // *从缓存池取资源
    ResourceItem GetCacheResourceItem(uint crc, int addrefcount = 1)
    {
        if (AssetDic.TryGetValue(crc, out var item))
        {
            if (item != null)
            {
                item.RefCount += addrefcount;
                item.m_LastUseTime = Time.realtimeSinceStartup;              
            }
        }
        return item;
    }

    // *放入缓存池
    protected void CacheResource(string path, ref ResourceItem item, uint crc, Object obj, int addrefcount = 1)
    {
        if (item == null)
        {
            Debug.LogError("ResourceItem is null,path:" + path);

        }
        if (obj == null)
        {
            Debug.LogError("ResourceLoad Fail:" + path);
        }

        item.m_Obj = obj;
        item.m_Guid = obj.GetInstanceID();
        item.m_LastUseTime = Time.realtimeSinceStartup;
        item.RefCount += addrefcount;
        if (AssetDic.TryGetValue(item.m_Crc, out var oldItem))
        {
            AssetDic[item.m_Crc] = item;
        }
        else
        {
            AssetDic.Add(item.m_Crc, item);
        }

    }

    // *清空缓存池
    public void ClearCache()
    {
        List<ResourceItem> tempList = new List<ResourceItem>();
        AssetDic.Values.ToList().ForEach(x =>
        {
            if (x.m_Clear)
                tempList.Add(x);
        });

        tempList.ForEach(x => DestoryResourceItem(x, true));

        tempList.Clear();

    }

    // *缓存太多，清除最早没用的资源
    protected void WashOut()
    {
        //当大于缓存个数时，进行一半释放
        while (m_NoRefrenceAssetMapList.Size() >= MAXCACHECOUNT)
        {
            for (int i = 0; i < MAXCACHECOUNT / 2; i++)
            {
                ResourceItem item = m_NoRefrenceAssetMapList.Trail();
                DestoryResourceItem(item, true);
            }

        }
    }
    #endregion


}




