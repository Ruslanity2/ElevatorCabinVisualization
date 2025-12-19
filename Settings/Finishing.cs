using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ElevatorCabinVisualization
{
    [XmlRoot("Finishing")]
    public class Finishing
    {
        [XmlElement("Group")]
        public List<FinishingGroup> Groups { get; set; } = new List<FinishingGroup>();

        /// <summary>
        /// Загрузить XML-файл
        /// </summary>
        public static Finishing Load(string xmlFilePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Finishing));

            if (!File.Exists(xmlFilePath))
                return new Finishing();

            using (StreamReader reader = new StreamReader(xmlFilePath))
            {
                return (Finishing)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Сохранить в XML-файл
        /// </summary>
        public void Save(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Finishing));
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }

        /// <summary>
        /// Получить группу по имени
        /// </summary>
        public FinishingGroup GetGroupByName(string name)
        {
            return Groups.Find(g => g.Name == name);
        }
    }

    public class FinishingGroup
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlElement("Row")]
        public List<FinishingRow> Rows { get; set; } = new List<FinishingRow>();
    }

    public class FinishingRow
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("PathModel")]
        public string PathModel { get; set; }

        [XmlAttribute("PathImage")]
        public string PathImage { get; set; }

        [XmlAttribute("PathXml")]
        public string PathXml { get; set; }
    }
}
