

namespace MmInventory
{
    public interface IGridAudioAndAnimation
    {
        // 下面这两个是一样的 都是鼠标进出物品时触发
        void OnMouseEnterItem();
        void OnMouseExitItem();

        // 下面这两个音效是根据物品不同来配置的
        void OnSelectItem();
        void OnDeselectItem();
    }
}