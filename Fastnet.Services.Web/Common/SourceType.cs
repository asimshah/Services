namespace Fastnet.Services.Web
{
    public enum SourceType
    {
        StandardSource, // copied every day into a fresh zip file
        Website, // copied every day into a fresh zip file after closing the site (and then reopening it!)
        ReplicationSource // contents replicated to a destination
    }
}
