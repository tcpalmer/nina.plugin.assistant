using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assistant.NINAPlugin.Database.Schema {

    public class ImageData {

        [Key] public int Id { get; set; }
        public string tag { get; set; }
        public byte[] imagedata { get; set; }

        [ForeignKey("AcquiredImage")] public int AcquiredImageId { get; set; }
        public virtual AcquiredImage AcquiredImage { get; set; }

        [NotMapped]
        public string Tag {
            get => tag; set { tag = value; }
        }

        [NotMapped]
        public byte[] Data {
            get => imagedata; set { imagedata = value; }
        }

        public ImageData() { }

        public ImageData(string tag, byte[] data, int acquiredImageId) {
            Tag = tag;
            Data = data;
            AcquiredImageId = acquiredImageId;
        }

    }
}
