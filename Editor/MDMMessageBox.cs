using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Modoium.Service.Editor {
    internal class MDMMessageBox : Box {
        public enum Icon {
            Warning
        }

        public MDMMessageBox() {
            this.Padding(6);
            style.flexDirection = FlexDirection.Row;
            style.marginTop = style.marginBottom = 10;
            style.minHeight = 44;
        }

        public void SetContent(Icon icon, string body) {
            Clear();

            Add(createIcon(icon));
            Add(createBody(body));
        }

        private VisualElement createIcon(Icon icon) {
            var element = new VisualElement();
            element.style.width = element.style.height = 32;
            element.style.backgroundImage = icon switch {
                Icon.Warning => EditorGUIUtility.IconContent("Warning@2x").image as Texture2D,
                _ => null
            };

            return element;
        }

        private VisualElement createBody(string body) {
            var container = new VisualElement().FillParent();
            container.style.justifyContent = Justify.Center;
            
            container.Add(new TextElement { text = body });

            return container;
        }
    }
}
