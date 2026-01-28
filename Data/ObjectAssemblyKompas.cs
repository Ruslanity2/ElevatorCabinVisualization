using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorCabinVisualization
{
    public class ObjectAssemblyKompas
    {
        public string Designation { get; set; }
        public string NewDesignation { get; set; }
        public string Name { get; set; }
        public string NewName { get; set; }
        public string FullName { get; set; }
        public string NewFullName { get; set; }
        public string DxfFilePath { get; set; }
        public string PdfFilePath { get; set; }
        public bool IsLocal { get; set; }
        public string SpecificationSection { get; set; }
        public bool IsDetail { get; set; }
        public List<string> DrawingReferences { get; set; }
        public string NewDrawingName { get; set; }

        // Матрица трансформации 4x4 из IPlacement3D.GetMatrix3D()
        // Содержит и расположение, и ориентацию детали в сборке
        public double[] TransformMatrix { get; set; }

        // Флаги свойств из KOMPAS
        public bool NeedsRework { get; set; }
        public bool ModifierFastener { get; set; }

        // Связи в дереве
        public ObjectAssemblyKompas Parent { get; set; }
        public List<ObjectAssemblyKompas> Children { get; private set; }

        public ObjectAssemblyKompas() { }

        // Добавить ребёнка
        public void AddChild(ObjectAssemblyKompas child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            // Проверка, инициализирован ли список Children
            if (Children == null)
            {
                Children = new List<ObjectAssemblyKompas>();
            }

            // Поиск в коллекции по совпадению поля Name и Designation
            var existingChild = Children.FirstOrDefault(c =>
                c.Name == child.Name && c.Designation == child.Designation);

            if (existingChild != null)
            {
                return;
            }
            else
            {
                // Если не найден, добавляем новый объект
                child.Parent = this;
                Children.Add(child);
            }
        }

        // Удалить ребёнка
        public bool RemoveChild(ObjectAssemblyKompas child)
        {
            if (child == null)
                return false;

            if (Children.Remove(child))
            {
                child.Parent = null;
                return true;
            }
            return false;
        }

        // Найти узел по имени (рекурсивно)
        public ObjectAssemblyKompas FindChild(string designation = null, string name = null)
        {
            // Проверяем текущий узел
            bool matchesDesignation = string.IsNullOrEmpty(designation) || (Designation != null && Designation.Contains(designation));
            bool matchesName = string.IsNullOrEmpty(name) || (Name != null && Name.Contains(name));

            //if (matchesDesignation && matchesName)
            //{
            //    return this;
            //}
            // Рекурсивно ищем в дочерних узлах
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    var found = child.FindChild(designation, name);
                    if (found != null)
                        return found; // Немедленный возврат при первом совпадении
                }
            }

            return null; // Если ничего не найдено
        }
    }
}
