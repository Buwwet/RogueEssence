﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using RogueElements;
using System.IO;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using RogueEssence.Dev.Views;
using Microsoft.Xna.Framework;
using Avalonia.Interactivity;
using Avalonia;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Avalonia.Data.Converters;
using System.Text;

namespace RogueEssence.Dev
{
    public static class DataEditor
    {
        private static List<IEditor> editors;

        public static object clipboardObj;

        public static void Init()
        {
            clipboardObj = new object();
            editors = new List<IEditor>();
        }

        public static void AddEditor(IEditor editor)
        {
            //maintain inheritance order
            for (int ii = 0; ii < editors.Count; ii++)
            {
                if (editor.GetConvertingType().IsSubclassOf(editors[ii].GetConvertingType()))
                {
                    editors.Insert(ii, editor);
                    return;
                }
                else if (editor.GetConvertingType() == editors[ii].GetConvertingType() && editor.GetAttributeType() != null && editors[ii].GetAttributeType() == null)
                {
                    editors.Insert(ii, editor);
                    return;
                }
            }
            editors.Add(editor);
        }

        public static void LoadDataControls(string assetName, object obj, DataEditForm editor)
        {
            Type editType = obj.GetType();
            LoadClassControls(editor.ControlPanel, assetName, null, obj.ToString(), editType, new object[0], obj, true, new Type[0]);
            TrackTypeSize(editor, editType);
        }

        public static void TrackTypeSize(DataEditForm editor, Type editType)
        {
            Size savedSize;
            if (DevDataManager.GetTypeSize(editType, out savedSize))
            {
                editor.Width = savedSize.Width;
                editor.Height = savedSize.Height;
            }

            void editorWindow_SizeChanged(Size size)
            {
                DevDataManager.SetTypeSize(editType, size);
            }

            editor.GetObservable(TopLevel.ClientSizeProperty).Subscribe(editorWindow_SizeChanged);
        }

        private static IEditor findEditor(Type objType, object[] attributes)
        {
            foreach (IEditor editor in editors)
            {
                Type editType = editor.GetConvertingType();
                if (editType.IsAssignableFrom(objType))
                {
                    Type attrType = editor.GetAttributeType();
                    if (attrType == null)
                        return editor;
                    else
                    {
                        foreach (object attr in attributes)
                        {
                            if (attr.GetType() == attrType)
                                return editor;
                        }
                    }
                }
            }
            throw new ArgumentException("Unhandled type!");
        }

        public static void LoadClassControls(StackPanel control, string parent, Type parentType, string name, Type type, object[] attributes, object member, bool isWindow, Type[] subGroupStack)
        {
            IEditor converter = findEditor(type, attributes);
            converter.LoadClassControls(control, parent, parentType, name, type, attributes, member, isWindow, subGroupStack);
        }

        public static void LoadWindowControls(StackPanel control, string parent, Type parentType, string name, Type type, object[] attributes, object obj, Type[] subGroupStack)
        {
            IEditor converter = findEditor(type, attributes);
            converter.LoadWindowControls(control, parent, parentType, name, type, attributes, obj, subGroupStack);
        }

        public static void LoadMemberControl(string parent, object obj, StackPanel control, string name, Type type, object[] attributes, object member, bool isWindow, Type[] subGroupStack)
        {
            IEditor converter = findEditor(obj.GetType(), attributes);
            converter.LoadMemberControl(parent, obj, control, name, type, attributes, member, isWindow, subGroupStack);
        }

        public static void SaveDataControls(ref object obj, StackPanel control, Type[] subGroupStack)
        {
            obj = SaveClassControls(control, obj.ToString(), obj.GetType(), new object[0], true, subGroupStack);
        }

        public static object SaveClassControls(StackPanel control, string name, Type type, object[] attributes, bool isWindow, Type[] subGroupStack)
        {
            IEditor converter = findEditor(type, attributes);
            return converter.SaveClassControls(control, name, type, attributes, isWindow, subGroupStack);
        }


        public static object SaveWindowControls(StackPanel control, string name, Type type, object[] attributes, Type[] subGroupStack)
        {
            IEditor converter = findEditor(type, attributes);
            return converter.SaveWindowControls(control, name, type, attributes, subGroupStack);
        }


        public static object SaveMemberControl(object obj, StackPanel control, string name, Type type, object[] attributes, bool isWindow, Type[] subGroupStack)
        {
            IEditor converter = findEditor(obj.GetType(), attributes);
            return converter.SaveMemberControl(obj, control, name, type, attributes, isWindow, subGroupStack);
        }

        public static string GetString(object obj, Type type, object[] attributes)
        {
            if (obj == null)
                return "NULL";
            IEditor editor = findEditor(obj.GetType(), attributes);
            return editor.GetString(obj, type, attributes);
        }

        public static string GetWindowTitle(string parent, string name, object obj, Type type)
        {
            return GetWindowTitle(parent, name, obj, type, new object[0]);
        }

        public static string GetWindowTitle(string parent, string name, object obj, Type type, object[] attributes)
        {
            string parentStr = Text.GetMemberTitle(parent);
            string nameStr = Text.GetMemberTitle(name);

            //if (obj == null)
            //    return String.Format("{0}.{1}: New {2}", parentStr, nameStr, type.Name);
            //else
            //    return String.Format("{0}.{1}: {2}", parentStr, nameStr, DataEditor.GetString(obj, type, attributes));
            return String.Format("{0}: {1}", parentStr, nameStr);
        }

        public static void SetClipboardObj(object obj)
        {
            clipboardObj = ReflectionExt.SerializeCopy(obj);
        }
    }
}

