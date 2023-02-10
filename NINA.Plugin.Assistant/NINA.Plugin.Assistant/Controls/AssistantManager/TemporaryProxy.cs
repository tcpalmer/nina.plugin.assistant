﻿using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using System;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class TemporaryProxy<T> : BaseINPC where T : ICloneable {

        private T original;
        private T proxy;


        public TemporaryProxy(T original) {
            this.original = original;
            this.Proxy = (T)original.Clone();
        }

        public T Original {
            get => original;
            set {
                original = value;
                RaiseAllPropertiesChanged();
            }
        }

        public T Proxy {
            get => proxy;
            set {
                proxy = value;
                RaiseAllPropertiesChanged();
            }
        }

        public T GetOriginalObject() {
            return original;
        }

        public T GetEditedObject() {
            return Proxy;
        }
    }

    public class ProjectProxy : TemporaryProxy<Project> {

        public ProjectProxy(Project project) : base(project) {
        }

        public Project Project {
            get => Proxy;
            set {
                Proxy = value;
                RaisePropertyChanged(nameof(Proxy));
            }
        }

    }
}
