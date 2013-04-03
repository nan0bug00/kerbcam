﻿using KSP.IO;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbCam {
    public delegate void KeyEvent();
    public delegate void AnyKeyEvent(Event ev);

    class KeyBind<KeyT> {
        private Event binding;
        private string humanBinding;
        private Event defaultBind;
        private KeyT key;
        public string description;
        public event KeyEvent ev;

        public KeyBind(KeyT key, string description, KeyCode defaultKeyCode) {
            this.key = key;
            this.description = description;
            this.defaultBind = EventHelper.KeyboardUpEvent(defaultKeyCode.ToString());
            SetBinding(defaultBind);
        }

        public KeyBind(KeyT key, string description, Event defaultBind) {
            this.key = key;
            this.description = description;
            this.defaultBind = defaultBind;
            SetBinding(defaultBind);
        }

        public void SetBinding(Event ev) {
            if (ev != null) {
                binding = new Event(ev);
                humanBinding = EventHelper.KeyboardEventHumanString(binding);
            } else {
                binding = null;
                humanBinding = "<unbound>";
            }
        }

        public KeyT Key {
            get { return key; }
        }

        public string HumanBinding {
            get { return humanBinding; }
        }

        public bool MatchAndFireEvent(Event ev) {
            if (this.binding != null && this.binding.Equals(ev)) {
                if (this.ev != null) {
                    this.ev();
                }
                ev.Use();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Loads the key binding from the configuration node.
        /// </summary>
        /// <param name="config">The node to load from. Can be null.</param>
        public void LoadFromConfig(PluginConfigNode config) {
            string evStr = null;
            if (config != null) {
                evStr = config.GetValue<string>(key.ToString());
            }
            if (evStr == null) {
                // No binding set yet.
                SetBinding(defaultBind);
            } else if (evStr == "") {
                // Explicitly unset.
                SetBinding(null);
            } else {
                // Configured.
                SetBinding(EventHelper.KeyboardUpEvent(evStr));
            }
        }

        /// <summary>
        /// Saves the key binding to the configuration node.
        /// </summary>
        /// <param name="config">The node to save to. Must not be null.</param>
        public void SaveToConfig(PluginConfigNode config) {
            string evStr;
            if (binding == null) {
                evStr = "";
            } else {
                evStr = EventHelper.KeyboardEventString(binding);
            }
            config.SetValue(key.ToString(), evStr);
        }
    }

    class KeyBindings<KeyT> {
        // TODO: Maybe optimize this with a hash of the binding, but be
        // careful about hashes changing when the binding changes.
        private List<KeyBind<KeyT>> bindings =
            new List<KeyBind<KeyT>>();
        private Dictionary<KeyT, KeyBind<KeyT>> keyToBinding =
            new Dictionary<KeyT, KeyBind<KeyT>>();

        /// <summary>
        /// Captures *all* key events. Will block other key events while at
        /// least one delegate is set.
        /// </summary>
        public event AnyKeyEvent captureAnyKey;

        public void AddBinding(KeyBind<KeyT> kb) {
            this.bindings.Add(kb);
            keyToBinding[kb.Key] = kb;
        }

        public void Listen(KeyT key, KeyEvent del) {
            keyToBinding[key].ev += del;
        }

        public void Unlisten(KeyT key, KeyEvent del) {
            keyToBinding[key].ev -= del;
        }

        public void HandleEvent(Event ev) {
            if (ev.isKey && ev.type == EventType.KeyUp) {
                if (captureAnyKey != null) {
                    captureAnyKey(ev);
                    ev.Use();
                } else {
                    foreach (var kb in bindings) {
                        if (kb.MatchAndFireEvent(ev)) {
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads the key bindings from the configuration node.
        /// </summary>
        /// <param name="config">The node to load from. Can be null.</param>
        public void LoadFromConfig(PluginConfigNode config) {
            foreach (var kb in bindings) {
                kb.LoadFromConfig(config);
            }
        }

        /// <summary>
        /// Saves the key bindings to the configuration node.
        /// </summary>
        /// <param name="config">The node to save to. Must not be null.</param>
        public void SaveToConfig(PluginConfigNode config) {
            foreach (var kb in bindings) {
                kb.SaveToConfig(config);
            }
        }

        public IEnumerable<KeyBind<KeyT>> Bindings() {
            return bindings;
        }
    }

    public class EventHelper {
        /// <summary>
        /// Reverse operation of Event.KeyboardEvent/KeyboardUpEvent.
        /// </summary>
        /// <param name="ev">The event to turn into a string.</param>
        /// <returns>The event as a string.</returns>
        public static string KeyboardEventString(Event ev) {
            if (!ev.isKey) {
                throw new Exception("Not a keyboard event: " + ev.ToString());
            }

            StringBuilder s = new StringBuilder(10);
            var mods = ev.modifiers;
            if ((mods & EventModifiers.Alt) != 0) s.Append("&");
            if ((mods & EventModifiers.Control) != 0) s.Append("^");
            if ((mods & EventModifiers.Command) != 0) s.Append("%");
            if ((mods & EventModifiers.Shift) != 0) s.Append("#");
            s.Append(ev.keyCode.ToString());

            return s.ToString();
        }

        public static Event KeyboardUpEvent(string evStr) {
            Event ev = Event.KeyboardEvent(evStr);
            ev.type = EventType.KeyUp;
            return ev;
        }

        /// <summary>
        /// Creates a readable string for the event.
        /// </summary>
        /// <param name="ev">The event to turn into a descriptive string.</param>
        /// <returns>The description string.</returns>
        public static string KeyboardEventHumanString(Event ev) {
            if (!ev.isKey) {
                throw new Exception("Not a keyboard event: " + ev.ToString());
            }

            List<string> p = new List<string>(5);
            var mods = ev.modifiers;
            if ((mods & EventModifiers.Alt) != 0) p.Add("Alt");
            if ((mods & EventModifiers.Control) != 0) p.Add("Ctrl");
            if ((mods & EventModifiers.Command) != 0) p.Add("Cmd");
            if ((mods & EventModifiers.Shift) != 0) p.Add("Shift");
            p.Add(ev.keyCode.ToString());

            return string.Join("+", p.ToArray());
        }
    }
}
