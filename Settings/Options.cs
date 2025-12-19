using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace CabinDesignTool
{
    [XmlRoot("Options")]
    public class Options
    {
        [XmlArray("Marks")]
        [XmlArrayItem("Mark")]
        public List<Mark> Marks { get; set; } = new List<Mark>();

        [XmlArray("Dimensions")]
        [XmlArrayItem("Dimension")]
        public List<Dimension> Dimensions { get; set; } = new List<Dimension>();

        [XmlElement("OptionGroup")]
        public List<OptionGroup> OptionGroups { get; set; } = new List<OptionGroup>();

        /// <summary>
        /// Загрузить XML-файл
        /// </summary>
        public static Options Load(string xmlFilePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Options));

            if (!File.Exists(xmlFilePath))
                return new Options();

            using (StreamReader reader = new StreamReader(xmlFilePath))
            {
                return (Options)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Сохранить в XML-файл
        /// </summary>
        public void Save(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Options));
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }
    }

    public class Mark
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("mark")]
        public string MarkType { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }
    }

    public class Dimension
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("dimension")]
        public string DimensionCode { get; set; }

        [XmlAttribute("difference")]
        public double Difference { get; set; }

        [XmlAttribute("min")]
        public double Min { get; set; }

        [XmlAttribute("max")]
        public double Max { get; set; }

        [XmlAttribute("inc")]
        public double Inc { get; set; }

        [XmlAttribute("default")]
        public double Default { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("value")]
        public double Value { get; set; }
    }

    public class OptionGroup
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("Option")]
        public List<Option> Options { get; set; } = new List<Option>();
    }

    public class Option
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("default")]
        public bool Default { get; set; }

        [XmlArray("Dimensions")]
        [XmlArrayItem("Dimension")]
        public List<Dimension> Dimensions { get; set; } = new List<Dimension>();
    }
}
