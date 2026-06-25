using System;
using UnityEngine;
namespace MmInventory
{
    /// <summary>
    /// 运行时物品数据
    /// </summary>
    public class ItemRtData
    {
        /// <summary>
        /// 持久化物品ID
        /// </summary>
        [SerializeField] private int persistenceItemId;

        /// <summary>
        /// 物品在背包中的锚点位置
        /// </summary>
        [SerializeField] private Vector2Int anchorPos;

        /// <summary>
        /// 物品当前尺寸
        /// </summary>
        [SerializeField] private Vector2Int dataSize;

        /// <summary>
        /// 当前堆叠数量
        /// </summary>
        [SerializeField] private int curStackCount;

        /// <summary>
        /// 注意旋转只有两种情况 0 和 90
        /// </summary>
        [SerializeField] private bool isRotated;

        /// <summary>
        /// 背包ID 只有容器类物品才有
        /// </summary>
        [SerializeField] private int containerId;

        /// <summary>
        /// 物品实例ID 用于唯一标识一个物品
        /// </summary>
        [SerializeField] private string instancedItemId;


        #region 属性
        public int PersistenceItemId => persistenceItemId;
        public Vector2Int AnchorPos => anchorPos;
        public Vector2Int DataSize => dataSize;
        public int CurStackCount => curStackCount;
        public bool IsRotated => isRotated;
        public int ContainerId => containerId;
        public string InstancedItemId => instancedItemId;
        #endregion

        #region 方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public ItemRtData(int persistenceItemId,
                               Vector2Int dataSize,
                               int curStackCount,
                               bool isRotated,
                               int containerId,
                               string instancedItemId = null)
        {
            this.persistenceItemId = persistenceItemId;
            this.dataSize = dataSize;
            this.curStackCount = curStackCount;
            this.isRotated = isRotated;

            this.containerId = containerId;
            this.instancedItemId = string.IsNullOrEmpty(instancedItemId) ?
                    Guid.NewGuid().ToString() : instancedItemId;
        }

        /// <summary>
        /// 设置物品在背包中的锚点位置
        /// </summary>
        /// <param name="newAnchorPos"> 新的锚点位置 </param>
        public void SetAnchorPos(Vector2Int newAnchorPos)
        {
            anchorPos = newAnchorPos;
        }

        /// <summary>
        /// 设置旋转状态
        /// </summary>
        /// <param name="isRotated"></param>
        public void SetRotated(bool isRotated)
        {
            this.isRotated = isRotated;
            // 交换宽高
            dataSize = new Vector2Int(dataSize.y, dataSize.x);
        }

        /// <summary>
        /// 设置堆叠数量
        /// </summary>
        /// <param name="count"></param>
        public void SetStackCount(int count)
        {
            curStackCount = Mathf.Max(0, count);
        }

        /// <summary>
        /// 设置当前所属背包容器ID
        /// </summary>
        /// <param name="newContainerId"></param>
        public void SetContainer(int newContainerId)
        {
            containerId = newContainerId;
        }
        #endregion
    }
}