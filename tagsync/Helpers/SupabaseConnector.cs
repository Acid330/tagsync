namespace tagsync.Helpers;

public static class SupabaseConnector
{
    private static Supabase.Client _client;

    public static Supabase.Client Client
    {
        get
        {
            if (_client == null)
            {
                var url = "https://xavaoddkhecbwpgljrzu.supabase.co";
                var key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InhhdmFvZGRraGVjYndwZ2xqcnp1Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0NDIwODA2OCwiZXhwIjoyMDU5Nzg0MDY4fQ.5rAHFtOo_5tuJynFrHaxysEzlFXkWMsIgWrMXAk22sU";
                _client = new Supabase.Client(url, key);
                _client.InitializeAsync().Wait();
            }

            return _client;
        }
    }
}
