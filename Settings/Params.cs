using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ElevatorCabinVisualization
{
    [XmlRoot("Params")]
    public class Params
    {
        [XmlArray("Marks")]
        [XmlArrayItem("Mark")]
        public List<Mark> Marks { get; set; } = new List<Mark>();

        [XmlArray("Parameters")]
        [XmlArrayItem("Parameter")]
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

        /// <summary>
        /// Загрузить XML-файл
        /// </summary>
        public static Params Load(string xmlFilePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Params));

            if (!File.Exists(xmlFilePath))
                return new Params();

            using (StreamReader reader = new StreamReader(xmlFilePath))
            {
                return (Params)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Сохранить в XML-файл
        /// </summary>
        public void Save(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Params));
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }

        /// <summary>
        /// Получить параметр по имени
        /// </summary>
        public Parameter GetParameterByName(string name)
        {
            return Parameters.Find(p => p.Name == name);
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

    public class Parameter
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("min")]
        public double Min { get; set; }

        [XmlAttribute("max")]
        public double Max { get; set; }

        [XmlAttribute("inc")]
        public double Inc { get; set; }

        [XmlAttribute("default")]
        public double Default { get; set; }
    }
}
