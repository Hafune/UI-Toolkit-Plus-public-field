using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace QuickEye.UIToolkit.Editor
{
    [UxmlElement]
    public partial class ToolbarDropdownButton : ToolbarButton, IToolbarMenuElement
    {
        public new const string ussClassName = "qe-toolbar-dropdown-button";
        public const string iconUssClassName = ussClassName + "__icon";
        public const string labelUssClassName = ussClassName + "__label";
        public const string spacerUssClassName = ussClassName + "__spacer";
        public const string dropdownAreaUssClassName = ussClassName + "__dropdown-area";

        private readonly TextElement _label;
        private readonly VisualElement _dropdownArea;
        
        public DropdownMenu menu { get; }

        [UxmlAttribute("text")]
        public new string text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public ToolbarDropdownButton()
        {
            this.InitResources();
            AddToClassList(ussClassName);
            style.flexDirection = FlexDirection.Row;
            
            menu = new DropdownMenu();

            _label = new TextElement();
            _label.AddToClassList(labelUssClassName);
            Add(_label);

            var spacer = new VisualElement();
            spacer.AddToClassList(spacerUssClassName);
            Add(spacer);

            var dropdownIcon = new VisualElement();
            dropdownIcon.AddToClassList(iconUssClassName);

            _dropdownArea = new VisualElement();
            _dropdownArea.RegisterCallback<MouseDownEvent>(evt =>
            {
                evt.PreventDefault();
                evt.StopImmediatePropagation();

                this.ShowMenu();
            });
            _dropdownArea.AddToClassList(dropdownAreaUssClassName);
            _dropdownArea.Add(dropdownIcon);
            Add(_dropdownArea);
        }

    }
}
