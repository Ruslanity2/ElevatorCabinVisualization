using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ElevatorCabinVisualization
{
    [XmlRoot("report")]
    public class Report
    {
        [XmlElement("part")]
        public List<ReportPart> Parts { get; set; } = new List<ReportPart>();

        /// <summary>
        /// Загрузить XML-файл
        /// </summary>
        public static Report Load(string xmlFilePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Report));

            if (!File.Exists(xmlFilePath))
                return new Report();

            using (StreamReader reader = new StreamReader(xmlFilePath))
            {
                return (Report)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Сохранить в XML-файл
        /// </summary>
        public void Save(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Report));
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }

        /// <summary>
        /// Получить часть по группе
        /// </summary>
        public ReportPart GetPartByGroup(string group)
        {
            return Parts.Find(p => p.Group == group);
        }
    }

    public class ReportPart
    {
        [XmlAttribute("Group")]
        public string Group { get; set; }

        [XmlAttribute("PathModel")]
        public string PathModel { get; set; }

        [XmlElement("Dimension")]
        public List<ReportDimension> Dimensions { get; set; } = new List<ReportDimension>();
    }

    public class ReportDimension
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("dimension")]
        public string Dimension { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}
