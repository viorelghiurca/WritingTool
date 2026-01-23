using System;
using System.Collections.Generic;

namespace WritingTool.Services
{
    /// <summary>
    /// Service for managing application localization with support for English, German, and Romanian.
    /// </summary>
    public static class LocalizationService
    {
        public const string English = "en";
        public const string German = "de";
        public const string Romanian = "ro";

        private static string _currentLanguage = English;

        /// <summary>
        /// Event raised when the language changes.
        /// </summary>
        public static event Action? LanguageChanged;

        /// <summary>
        /// Gets or sets the current language code (en, de, ro).
        /// </summary>
        public static string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value && IsValidLanguage(value))
                {
                    _currentLanguage = value;
                    LanguageChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Checks if the language code is valid.
        /// </summary>
        public static bool IsValidLanguage(string language)
        {
            return language == English || language == German || language == Romanian;
        }

        /// <summary>
        /// Gets the display name for a language code.
        /// </summary>
        public static string GetLanguageDisplayName(string languageCode)
        {
            return languageCode switch
            {
                English => "English",
                German => "Deutsch",
                Romanian => "Română",
                _ => "English"
            };
        }

        /// <summary>
        /// Gets a localized string by key.
        /// </summary>
        public static string Get(string key)
        {
            if (Strings.TryGetValue(_currentLanguage, out var langStrings) &&
                langStrings.TryGetValue(key, out var value))
            {
                return value;
            }

            // Fallback to English
            if (Strings.TryGetValue(English, out var enStrings) &&
                enStrings.TryGetValue(key, out var enValue))
            {
                return enValue;
            }

            return key;
        }

        /// <summary>
        /// Gets a localized button name. Falls back to the original name if not found.
        /// </summary>
        public static string GetButtonName(string originalName)
        {
            var key = $"btn_{originalName.ToLowerInvariant().Replace(" ", "_")}";
            var result = Get(key);
            return result == key ? originalName : result;
        }

        /// <summary>
        /// All localized strings organized by language.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            [English] = new Dictionary<string, string>
            {
                // App general
                ["app_name"] = "WritingTool",
                ["app_subtitle"] = "AI-Powered Writing Assistant",
                
                // Tray menu
                ["tray_show"] = "Show",
                ["tray_settings"] = "Settings",
                ["tray_about"] = "About",
                ["tray_exit"] = "Exit",
                
                // Main window
                ["main_describe_change"] = "Describe your change...",
                ["main_edit_buttons"] = "Edit buttons",
                ["main_add_button"] = "Add Button",
                ["main_move_here"] = "Move here",
                ["main_close"] = "Close (Esc)",
                ["main_send"] = "Send",
                
                // Ask window
                ["ask_title"] = "Ask AI",
                ["ask_placeholder"] = "Ask anything... (Ctrl+Enter to send)",
                ["ask_thinking"] = "Thinking...",
                ["ask_new_chat"] = "New conversation",
                ["ask_send"] = "Send",
                ["ask_close"] = "Close (Esc)",
                ["ask_copy_clipboard"] = "Copy to clipboard",
                
                // Settings window
                ["settings_title"] = "Settings",
                ["settings_subtitle"] = "Configure your AI writing assistant",
                ["settings_general"] = "General",
                ["settings_language"] = "Language",
                ["settings_start_on_boot"] = "Start on Boot",
                ["settings_enabled"] = "Enabled",
                ["settings_disabled"] = "Disabled",
                ["settings_shortcut"] = "Shortcut Key",
                ["settings_theme"] = "Background Theme",
                ["settings_provider"] = "AI Provider",
                ["settings_api_key"] = "API Key",
                ["settings_model"] = "Model",
                ["settings_api_base"] = "API Base URL",
                ["settings_organisation"] = "Organisation (optional)",
                ["settings_project"] = "Project (optional)",
                ["settings_keep_alive"] = "Keep Alive (minutes)",
                ["settings_get_api_key"] = "Get Gemini API Key",
                ["settings_save"] = "Save Settings",
                
                // About window
                ["about_title"] = "About WritingTool",
                ["about_version"] = "Version",
                ["about_stable"] = "Stable",
                ["about_developer"] = "Software Developer",
                ["about_certification"] = "IHK Certified IT Specialist for Application Development",
                ["about_skills"] = "Skills & Expertise",
                ["about_development"] = "Development",
                ["about_development_desc"] = "Full-Stack, AI/ML, DevOps",
                ["about_management"] = "Management",
                ["about_management_desc"] = "Agile, Scrum, Leadership",
                ["about_sysadmin"] = "Sysadmin",
                ["about_sysadmin_desc"] = "Windows, Linux, Cloud",
                ["about_design"] = "Design",
                ["about_design_desc"] = "Web Design, UX/UI",
                ["about_experience"] = "Experience",
                ["about_years"] = "4+ Years",
                ["about_exp_1"] = "Full-stack development and project leadership",
                ["about_exp_2"] = "AI development and Machine Learning projects",
                ["about_exp_3"] = "DevOps practices and CI/CD pipelines",
                ["about_exp_4"] = "System integration and automation",
                
                // Button names
                ["btn_proofread"] = "Proofread",
                ["btn_rewrite"] = "Rewrite",
                ["btn_friendly"] = "Friendly",
                ["btn_professional"] = "Professional",
                ["btn_concise"] = "Concise",
                ["btn_table"] = "Table",
                ["btn_key_points"] = "Key Points",
                ["btn_summary"] = "Summary",
                ["btn_auf_deutsch"] = "To German",
            },
            
            [German] = new Dictionary<string, string>
            {
                // App general
                ["app_name"] = "WritingTool",
                ["app_subtitle"] = "KI-gestützter Schreibassistent",
                
                // Tray menu
                ["tray_show"] = "Anzeigen",
                ["tray_settings"] = "Einstellungen",
                ["tray_about"] = "Über",
                ["tray_exit"] = "Beenden",
                
                // Main window
                ["main_describe_change"] = "Änderung beschreiben...",
                ["main_edit_buttons"] = "Schaltflächen bearbeiten",
                ["main_add_button"] = "Schaltfläche hinzufügen",
                ["main_move_here"] = "Hierher verschieben",
                ["main_close"] = "Schließen (Esc)",
                ["main_send"] = "Senden",
                
                // Ask window
                ["ask_title"] = "KI fragen",
                ["ask_placeholder"] = "Frag etwas... (Strg+Enter zum Senden)",
                ["ask_thinking"] = "Denke nach...",
                ["ask_new_chat"] = "Neue Unterhaltung",
                ["ask_send"] = "Senden",
                ["ask_close"] = "Schließen (Esc)",
                ["ask_copy_clipboard"] = "In Zwischenablage kopieren",
                
                // Settings window
                ["settings_title"] = "Einstellungen",
                ["settings_subtitle"] = "KI-Schreibassistent konfigurieren",
                ["settings_general"] = "Allgemein",
                ["settings_language"] = "Sprache",
                ["settings_start_on_boot"] = "Beim Start starten",
                ["settings_enabled"] = "Aktiviert",
                ["settings_disabled"] = "Deaktiviert",
                ["settings_shortcut"] = "Tastenkürzel",
                ["settings_theme"] = "Hintergrund-Design",
                ["settings_provider"] = "KI-Anbieter",
                ["settings_api_key"] = "API-Schlüssel",
                ["settings_model"] = "Modell",
                ["settings_api_base"] = "API-Basis-URL",
                ["settings_organisation"] = "Organisation (optional)",
                ["settings_project"] = "Projekt (optional)",
                ["settings_keep_alive"] = "Keep Alive (Minuten)",
                ["settings_get_api_key"] = "Gemini API-Schlüssel holen",
                ["settings_save"] = "Einstellungen speichern",
                
                // About window
                ["about_title"] = "Über WritingTool",
                ["about_version"] = "Version",
                ["about_stable"] = "Stabil",
                ["about_developer"] = "Softwareentwickler",
                ["about_certification"] = "IHK-geprüfter Fachinformatiker für Anwendungsentwicklung",
                ["about_skills"] = "Fähigkeiten & Expertise",
                ["about_development"] = "Entwicklung",
                ["about_development_desc"] = "Full-Stack, KI/ML, DevOps",
                ["about_management"] = "Management",
                ["about_management_desc"] = "Agile, Scrum, Führung",
                ["about_sysadmin"] = "Systemadmin",
                ["about_sysadmin_desc"] = "Windows, Linux, Cloud",
                ["about_design"] = "Design",
                ["about_design_desc"] = "Webdesign, UX/UI",
                ["about_experience"] = "Erfahrung",
                ["about_years"] = "4+ Jahre",
                ["about_exp_1"] = "Full-Stack-Entwicklung und Projektleitung",
                ["about_exp_2"] = "KI-Entwicklung und Machine-Learning-Projekte",
                ["about_exp_3"] = "DevOps-Praktiken und CI/CD-Pipelines",
                ["about_exp_4"] = "Systemintegration und Automatisierung",
                
                // Button names
                ["btn_proofread"] = "Korrekturlesen",
                ["btn_rewrite"] = "Umschreiben",
                ["btn_friendly"] = "Freundlich",
                ["btn_professional"] = "Professionell",
                ["btn_concise"] = "Prägnant",
                ["btn_table"] = "Tabelle",
                ["btn_key_points"] = "Kernpunkte",
                ["btn_summary"] = "Zusammenfassung",
                ["btn_auf_deutsch"] = "Auf Deutsch",
            },
            
            [Romanian] = new Dictionary<string, string>
            {
                // App general
                ["app_name"] = "WritingTool",
                ["app_subtitle"] = "Asistent de scriere bazat pe IA",
                
                // Tray menu
                ["tray_show"] = "Afișează",
                ["tray_settings"] = "Setări",
                ["tray_about"] = "Despre",
                ["tray_exit"] = "Ieșire",
                
                // Main window
                ["main_describe_change"] = "Descrie modificarea...",
                ["main_edit_buttons"] = "Editează butoanele",
                ["main_add_button"] = "Adaugă buton",
                ["main_move_here"] = "Mută aici",
                ["main_close"] = "Închide (Esc)",
                ["main_send"] = "Trimite",
                
                // Ask window
                ["ask_title"] = "Întreabă IA",
                ["ask_placeholder"] = "Întreabă orice... (Ctrl+Enter pentru a trimite)",
                ["ask_thinking"] = "Gândesc...",
                ["ask_new_chat"] = "Conversație nouă",
                ["ask_send"] = "Trimite",
                ["ask_close"] = "Închide (Esc)",
                ["ask_copy_clipboard"] = "Copiază în clipboard",
                
                // Settings window
                ["settings_title"] = "Setări",
                ["settings_subtitle"] = "Configurează asistentul de scriere IA",
                ["settings_general"] = "General",
                ["settings_language"] = "Limbă",
                ["settings_start_on_boot"] = "Pornire la boot",
                ["settings_enabled"] = "Activat",
                ["settings_disabled"] = "Dezactivat",
                ["settings_shortcut"] = "Tastă rapidă",
                ["settings_theme"] = "Temă fundal",
                ["settings_provider"] = "Furnizor IA",
                ["settings_api_key"] = "Cheie API",
                ["settings_model"] = "Model",
                ["settings_api_base"] = "URL bază API",
                ["settings_organisation"] = "Organizație (opțional)",
                ["settings_project"] = "Proiect (opțional)",
                ["settings_keep_alive"] = "Keep Alive (minute)",
                ["settings_get_api_key"] = "Obține cheie API Gemini",
                ["settings_save"] = "Salvează setările",
                
                // About window
                ["about_title"] = "Despre WritingTool",
                ["about_version"] = "Versiune",
                ["about_stable"] = "Stabil",
                ["about_developer"] = "Dezvoltator Software",
                ["about_certification"] = "Specialist IT certificat IHK pentru dezvoltarea aplicațiilor",
                ["about_skills"] = "Abilități și expertiză",
                ["about_development"] = "Dezvoltare",
                ["about_development_desc"] = "Full-Stack, IA/ML, DevOps",
                ["about_management"] = "Management",
                ["about_management_desc"] = "Agile, Scrum, Leadership",
                ["about_sysadmin"] = "Sysadmin",
                ["about_sysadmin_desc"] = "Windows, Linux, Cloud",
                ["about_design"] = "Design",
                ["about_design_desc"] = "Web Design, UX/UI",
                ["about_experience"] = "Experiență",
                ["about_years"] = "4+ Ani",
                ["about_exp_1"] = "Dezvoltare full-stack și conducere de proiecte",
                ["about_exp_2"] = "Dezvoltare IA și proiecte Machine Learning",
                ["about_exp_3"] = "Practici DevOps și pipeline-uri CI/CD",
                ["about_exp_4"] = "Integrare sisteme și automatizare",
                
                // Button names
                ["btn_proofread"] = "Corectează",
                ["btn_rewrite"] = "Rescrie",
                ["btn_friendly"] = "Prietenos",
                ["btn_professional"] = "Profesional",
                ["btn_concise"] = "Concis",
                ["btn_table"] = "Tabel",
                ["btn_key_points"] = "Puncte cheie",
                ["btn_summary"] = "Rezumat",
                ["btn_auf_deutsch"] = "În germană",
            }
        };
    }
}
