﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OneLine {
	internal class SlicesCache {

		private Dictionary<string, Slices> cache;
		private Action<SerializedProperty, Slices> calculate;

		public bool IsDirty { get; private set; }

		public SlicesCache(Action<SerializedProperty, Slices> calculate){
			cache = new Dictionary<string, Slices>();
			this.calculate = calculate;
		}

		public Slices this[SerializedProperty property] {
			get {
				var id = GetId(property);
				if (cache.ContainsKey(id)){
					return cache[id];
				}
				else {
					var slices = new Slices();
					calculate(property, slices);
					cache.Add(id, slices);
					IsDirty = true;
					return slices;
				}
			}
		}

		private string GetId(SerializedProperty property){
			var paths = property.propertyPath.Split('.');
			string path = paths[0];
			if (paths.Length > 1 && paths[1] == "Array") {
				path = paths[0] + "." + paths[2];
			}
			return property.serializedObject.targetObject.GetInstanceID() + "->" + path;
		}

		public void Invalidate(SerializedProperty property){
			var id = GetId(property);
			cache.Remove(id);
		}

	}
}