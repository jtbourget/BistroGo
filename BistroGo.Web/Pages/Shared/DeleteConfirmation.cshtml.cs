// Deletes an item from the given array based on its ID. Returns true if the item was successfully found deleted, false otherwise.
public static class DeleteHelper
{
    public static bool DeleteItem<T>(List<T> itemList, int id) where T : class // Won't work, need a way to call a variable that has an Id property...
    {
        var existing = itemList.FirstOrDefault(i => i.Id == id);
        return existing != null && itemList.Remove(existing);
    }
}