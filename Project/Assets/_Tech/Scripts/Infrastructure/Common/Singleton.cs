public abstract class Singleton<Type> where Type : class, new()
{
    private static Type _instance;

    public static Type Instance
    {
        get
        {
            if (_instance == null)
                _instance = new Type();

            return _instance;
        }
    }
}