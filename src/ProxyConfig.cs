namespace CrmWebApiProxy
{
    public class ProxyConfig
    {
        public string ContextAuthority { get; set; }
        public string CrmHostUri { get; set; }
        public string WebApiUri { get; set; }
        public string ClientId { get; set; }
        public string TenantId { get; set; }
    }
}