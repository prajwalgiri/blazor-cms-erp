using MyErpApp.Core.Plugins;
using System.Composition;
using System.Text.Json;

namespace CommonUi.Plugin
{
    [Export(typeof(IUiComponentPlugin))]
    public class ButtonComponent : IUiComponentPlugin
    {
        public string Type => "Button";
        public string DisplayName => "Button";
        public string DefaultConfig() => "{\"Text\": \"Click Me\", \"Class\": \"px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors\"}";

        public string RenderHtml(string config)
        {
            var options = JsonSerializer.Deserialize<Dictionary<string, string>>(config) ?? new();
            var text = options.GetValueOrDefault("Text", "Click Me");
            var cssClass = options.GetValueOrDefault("Class", "px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors");
            return $"<button class=\"{cssClass}\">{text}</button>";
        }
    }

    [Export(typeof(IUiComponentPlugin))]
    public class InputComponent : IUiComponentPlugin
    {
        public string Type => "Input";
        public string DisplayName => "Input Box";
        public string DefaultConfig() => "{\"Label\": \"Name\", \"Placeholder\": \"Enter value...\", \"Type\": \"text\"}";

        public string RenderHtml(string config)
        {
            var options = JsonSerializer.Deserialize<Dictionary<string, string>>(config) ?? new();
            var label = options.GetValueOrDefault("Label", "Name");
            var placeholder = options.GetValueOrDefault("Placeholder", "Enter value...");
            var type = options.GetValueOrDefault("Type", "text");
            
            return $@"
                <div class=""mb-4"">
                    <label class=""block text-sm font-medium text-gray-700"">{label}</label>
                    <input type=""{type}"" placeholder=""{placeholder}"" class=""mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"">
                </div>";
        }
    }

    [Export(typeof(IUiComponentPlugin))]
    public class SelectComponent : IUiComponentPlugin
    {
        public string Type => "Select";
        public string DisplayName => "Dropdown List";
        public string DefaultConfig() => "{\"Label\": \"Options\", \"Items\": \"Value1:Label 1,Value2:Label 2\"}";

        public string RenderHtml(string config)
        {
            var options = JsonSerializer.Deserialize<Dictionary<string, object>>(config) ?? new();
            var label = options.ContainsKey("Label") ? options["Label"].ToString() : "Options";
            var itemsStr = options.ContainsKey("Items") ? options["Items"].ToString() : "";
            
            var items = itemsStr.Split(',').Select(i => i.Split(':')).Select(parts => new { 
                Value = parts.Length > 0 ? parts[0] : "", 
                Label = parts.Length > 1 ? parts[1] : (parts.Length > 0 ? parts[0] : "")
            });

            var optionsHtml = string.Join("", items.Select(i => $"<option value=\"{i.Value}\">{i.Label}</option>"));

            return $@"
                <div class=""mb-4"">
                    <label class=""block text-sm font-medium text-gray-700"">{label}</label>
                    <select class=""mt-1 block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md"">
                        {optionsHtml}
                    </select>
                </div>";
        }
    }

    [Export(typeof(IUiComponentPlugin))]
    public class CheckboxComponent : IUiComponentPlugin
    {
        public string Type => "Checkbox";
        public string DisplayName => "Checkbox";
        public string DefaultConfig() => "{\"Label\": \"Agree to terms\", \"Checked\": \"false\"}";

        public string RenderHtml(string config)
        {
            var options = JsonSerializer.Deserialize<Dictionary<string, string>>(config) ?? new();
            var label = options.GetValueOrDefault("Label", "Agree to terms");
            var isChecked = options.GetValueOrDefault("Checked", "false") == "true" ? "checked" : "";
            
            return $@"
                <div class=""flex items-start mb-4"">
                    <div class=""flex items-center h-5"">
                        <input type=""checkbox"" {isChecked} class=""focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"">
                    </div>
                    <div class=""ml-3 text-sm"">
                        <label class=""font-medium text-gray-700"">{label}</label>
                    </div>
                </div>";
        }
    }

    [Export(typeof(IUiComponentPlugin))]
    public class HeadingComponent : IUiComponentPlugin
    {
        public string Type => "Heading";
        public string DisplayName => "Heading";
        public string DefaultConfig() => "{\"Text\": \"Title\", \"Level\": \"1\"}";

        public string RenderHtml(string config)
        {
            var options = JsonSerializer.Deserialize<Dictionary<string, string>>(config) ?? new();
            var text = options.GetValueOrDefault("Text", "Title");
            var level = options.GetValueOrDefault("Level", "1");
            return $"<h{level} class=\"text-2xl font-bold mb-4\">{text}</h{level}>";
        }
    }
}
