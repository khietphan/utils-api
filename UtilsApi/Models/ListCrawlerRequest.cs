using System;
using Newtonsoft.Json;

namespace UtilsApi.Models
{
	public class ListCrawlerRequest
    {
        public string Endpoint { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        public string Body { get; set; }

        public int? ItemsPerPage { get; set; }

        public int? SkipTo { get; set; }
    }

    public class ListCrawlerResponse
    {
        public Dictionary<string, ListCrawlerDataListResponse> Data { get; set; }
    }

    public class ListCrawlerDataListResponse
    {
        public List<ListCrawlerDataListItemResponse> List { get; set; }
        public int TotalCount { get; set; }
    }

    public class ListCrawlerDataListItemResponse
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string IsActive { get; set; }
        public AssociatedUser AssociatedUser { get; set; }
    }

    public interface IAssociatedUser
    {
        public string Name { get; set; }
        public string Source { get; set; }
    }

    public class AssociatedUser : IAssociatedUser
    {
        public string Name { get; set; }
        public string Fields { get; set; }
        public string Source { get; set; }
        public AssociatedUserField FieldObject {
            get {
                return string.IsNullOrEmpty(Fields) ? null : JsonConvert.DeserializeObject<AssociatedUserField>(Fields);
            }
        }
    }

    public interface IAssociatedUserField
    {
        public string StaffId { get; set; }
        public string AgentCode { get; set; }
        public string SaleUserId { get; set; }
        public string AgentCodeName { get; set; }
    }

    public class AssociatedUserField
    {
        public string StaffId { get; set; }
        public string AgentCode { get; set; }
        public string SaleUserId { get; set; }
        public string AgentCodeName { get; set; }
    }

    public class ExcelRecord : IAssociatedUser, IAssociatedUserField
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string IsActive { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public string StaffId { get; set; }
        public string AgentCode { get; set; }
        public string SaleUserId { get; set; }
        public string AgentCodeName { get; set; }
    }
}

