public static class ArrayExtensions {
    public static int IndexOf<T>(this T[] array, Func<T, bool> predicate) {
        int index = 0;
        foreach (T item in array) {
            if (predicate(item)) {
                return index;
            }
            index++;
        }
        return -1;
    }
}

