using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Defines the WebHook registration data model for rows stored in SQL.
    /// </summary>
    [Table("WebHooks")]
    public class Registration
    {
        /// <summary>
        /// Gets or sets the user ID for this WebHook registration.
        /// </summary>
        [Key]
        [StringLength(256)]
        [Column(Order = 0)]
        public string User { get; set; }

        /// <summary>
        /// Gets or sets the ID of this WebHook registration.
        /// </summary>
        [Key]
        [StringLength(64)]
        [Column(Order = 1)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the data included in this WebHook registration. Note that this is encrypted 
        /// as it contains sensitive information.
        /// </summary>
        [Required]
        public string ProtectedData { get; set; }

        /// <summary>
        /// Gets or sets a unique row version.
        /// </summary>
        [Timestamp]
        public byte[] RowVer { get; set; }
    }
}
