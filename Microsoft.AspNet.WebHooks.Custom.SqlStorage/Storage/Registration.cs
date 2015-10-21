using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.AspNet.WebHooks.Storage
{
    [Table("WebHooks")]
    public class Registration
    {
        [Key]
        [StringLength(256)]
        [Column(Order =0)]
        public string User { get; set; }

        [Key]
        [StringLength(256)]
        [Column(Order = 1)]
        public string Id { get; set; }

        [Required]
        public string ProtectedData { get; set; }

        [Timestamp]
        public byte[] RowVer { get; set; }
    }
}
