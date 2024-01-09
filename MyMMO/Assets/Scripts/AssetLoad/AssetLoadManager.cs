using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using Tools;

/// <summary>
/// 资源加载 管理类
/// </summary>

public class AssetLoadManager : SingletonMono<AssetLoadManager>
    {

        // *资源集，从配置中解析获取
        protected Dictionary<string, MyAssetInfo> assetInfoDic = new Dictionary<string, MyAssetInfo>();
        // *加载路径头部
        private string loadPathHear= "Assets/TCMAsset/Resources/";

        protected override void onInit()
        {
#if UNITY_EDITOR
            gameObject.name = "AssetLoadManager";
#endif
            GameRoot.AddToGameRoot(gameObject);
        }


        public  IEnumerator Init()
        {

            var recycleTrs = new GameObject();
            recycleTrs.transform.SetParent(transform);
            recycleTrs.transform.position = Vector3.zero;
            recycleTrs.gameObject.SetActive(false);

            var sceneTrs = new GameObject();
            sceneTrs.transform.SetParent(transform);
            sceneTrs.transform.position = Vector3.zero;            
            ResourceManager.Instance.Init(this,recycleTrs.transform,sceneTrs.transform);
            
            yield return StartCoroutine(LoadConfigAsync());
        }

        public GameObject LoadGameObject(string prefabName,Transform parent=null)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                Debug.LogError("path is null--");
            }

            Debug.Log($"即将从Resources文件中加载{prefabName}");
            
            
            var go = Instantiate(Resources.Load<GameObject>(prefabName));

            if (parent)
            {
                go.transform.SetParent(parent);
            }
            
            go.transform.localPosition=Vector3.zero;
            return go;
        }

        public void RecyclePrefab(Object obj)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
            else
            {
                Debug.LogError($" Unable to delete, possibly null object . Name: {obj.name}");
            }
        }


        /*------------------------------------加载--------------------------------------*/
        // *同步加载预制体
        public GameObject LoadPrefab(string prefabName,Transform parent=null, bool setScene = false,bool bClear=true)
		{
            if(assetInfoDic.TryGetValue(prefabName,out var info))
			{
                var obj = ResourceManager.Instance.LoadInstantiateResource(loadPathHear + info.assetResourcesPath, parent, setScene, bClear);
                return obj;
			}
			else
			{
                Debug.LogError($"{prefabName}不存在！");
                return null;
			}
		}

        // *同步加载sprite(有图集)
        public Sprite LoadPicture(string atlasName,string spriteName)
		{
            if (string.IsNullOrEmpty(atlasName)) return null;
            var atlas = LoadAsset<SpriteAtlas>(atlasName);
            return atlas != null ? atlas.GetSprite(spriteName) : null;
        }

        // *同步加载sprite(无图集)
        public Sprite LoadPicture(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            var sprite = LoadAsset<Sprite>(spriteName);
            return sprite;
        }

        // *预加载预制体(路径 父物体 数量 是否跳场景清除)
        public void PreLoadPrefab(string path,Transform parent=null,int count=1,bool clear=false)
		{
            ResourceManager.Instance.PreloadInstantiateRes(path, parent, count, clear);
		}
        // *预加载不需要实例化的资源
        public void PreLoadAsset(string path)
		{
            ResourceManager.Instance.PreloadRes(path);
		}

		// *异步加载预制体(路径 完成回调 优先级 是否放到SceneTrs下 参数1 2 3 是否跳场景清除)
		public long AsyncLoadPrefab(string path,ResourceManager.OnAsyncObjFinish dealFinish, LoadResPriority priority=LoadResPriority.RES_SLOW, bool setSceneObject = false, object param1 = null,
	   object param2 = null, object param3 = null, bool bClear = true)
		{
            return ResourceManager.Instance.AsyncInstantiateObject(path, dealFinish, priority, setSceneObject,param1, param2, param3, bClear);
		}

        // *异步加载无需实例化资源
        public void AsyncLoadAsset(string path, ResourceManager.OnAsyncObjFinish dealFinish, LoadResPriority priority=LoadResPriority.RES_SLOW, object param1 = null,
        object param2 = null, object param3 = null, uint crc = 0)
		{
            ResourceManager.Instance.AsyncLoadResource(path, dealFinish, priority, param1, param2, param3, crc);
		}
        
        //同步加载 TextAsset
        public TextAsset LoadTextAsset(string fileName)
        {
            return LoadAsset<TextAsset>(fileName);
        }

        // *非实例化资源同步加载
		public T LoadAsset<T>(string name) where T : UnityEngine.Object
		{
            if(!assetInfoDic.TryGetValue(name,out var obj))
			{
                Debug.LogWarning($"没找到此资源:{name}");
                return null;
			}
            return ResourceManager.Instance.LoadResource<T>(loadPathHear + assetInfoDic[name].assetResourcesPath);
		}





        /*------------------------------------卸载（回收）--------------------------------------*/

        //暂只写prefab的回收（对象池回收）

        // *资源回收（存入对象池）(物体 缓存数量 是否清除缓存 是否回收到对象池节点)
        public void ReleasePrefab(GameObject obj, int maxCacheCount = 0, bool destoryCahe = false, bool recycleParent = true)
		{
            ResourceManager.Instance.ReleaseObject(obj, maxCacheCount, destoryCahe, recycleParent);
		}




        private IEnumerator LoadConfigAsync()
        {
            yield return StartCoroutine(RefreshAssetPathConfig());
        }

        // *加载预制体配置表
        private IEnumerator RefreshAssetPathConfig()
        {
            UnityWebRequest request = TextAssetUtil.GetTextAssetRequest("AssetPathConfig.json");
            yield return request.SendWebRequest();
            string pathConfigText = request.downloadHandler.text;

            JObject jsonData = JObject.Parse(pathConfigText);
            JArray assetConfigList = (JArray)jsonData["AssetPathConfig"];

            foreach (JToken item in assetConfigList)
            {
                JToken assetConfig = item;
                string assetName = ParseString(assetConfig["AssetName"]);
                if (!string.IsNullOrEmpty(assetName))
                {
                    MyAssetInfo assetInfo = null;
                    if (!assetInfoDic.TryGetValue(assetName, out assetInfo))
                    {
                        assetInfo = new MyAssetInfo();
                        assetInfoDic.Add(assetName, assetInfo);
                    }

                    assetInfo.assetName = ParseString(assetConfig["AssetName"]);
                    assetInfo.assetResourcesPath = ParseString(assetConfig["AssetPath"]);
                    assetInfo.assetType = ParseString(assetConfig["AssetType"]);
                }
            }
        }


        private string ParseString(JToken json)
        {
            if (json == null)
                return "";
            string res = json.ToString();
            return res;
        }




    }



    // *资源信息
    public class MyAssetInfo
    {
        public string assetName;
        public string assetResourcesPath;
        public string assetPath;
        public string assetType;
    }

