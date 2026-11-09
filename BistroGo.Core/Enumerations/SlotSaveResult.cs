namespace BistroGo.Core.Enumerations
{
    /// <summary>Outcome of saving a placed order into a slot with limited room.</summary>
    public enum SlotSaveResult
    {
        Saved,
        OrderNotFound,
        SlotFull
    }
}
