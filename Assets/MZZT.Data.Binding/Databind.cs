using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace MZZT.Data.Binding {
	public interface IDatabind {
		DatabindingDefaultValueStorageMethods ValueStorageMethod { get; set; }
		object Value { get; set; }
		event EventHandler BeforeValueChanged;
		event EventHandler ValueChanged;
		void Invalidate();
		event EventHandler Invalidated;
		IDatabind Parent { get; }
		object ParentValue { get; }
		string MemberName { get; set; }
		Type MemberType { get; }
	}

	public abstract class Databind : MonoBehaviour {
		protected readonly static HashSet<IDatabind> invalidationDeferred = new();

		protected static int invalidationDefers = 0;
		private static void BeginUpdate() {
			invalidationDefers++;
		}

		private static void EndUpdate() {
			if (--invalidationDefers > 0) {
				return;
			}
			foreach (IDatabind databind in invalidationDeferred.ToArray()) {
				invalidationDeferred.Remove(databind);
				databind.Invalidate();
				if (invalidationDefers > 0) {
					return;
				}
			}
		}

		public static void DeferInvalidation(Action callback) {
			BeginUpdate();
			try {
				callback();
			} finally {
				EndUpdate();
			}
		}

		public static T DeferInvalidation<T>(Func<T> callback) {
			BeginUpdate();
			try {
				return callback();
			} finally {
				EndUpdate();
			}
		}

		public static async Task DeferInvalidationAsync(Func<Task> callback) {
			BeginUpdate();
			try {
				await callback();
			} finally {
				EndUpdate();
			}
		}

		public static async Task<T> DeferInvalidationAsync<T>(Func<Task<T>> callback) {
			BeginUpdate();
			try {
				return await callback();
			} finally {
				EndUpdate();
			}
		}
	}

	public class Databind<T> : Databind, IDatabind {
		[Header("Value"), SerializeField]
		private DatabindingDefaultValueStorageMethods valueStorageMethod;
		public DatabindingDefaultValueStorageMethods ValueStorageMethod { get => this.valueStorageMethod; set => this.valueStorageMethod = value; }

		[SerializeField]
		private string playerPrefsKey;

		[SerializeField]
		private T value = default;
		object IDatabind.Value {
			get => this.Value;
			set {
				if (value is T tValue) {
					this.Value = tValue;
					return;
				}

				Type type = typeof(T);
				if (value == null) {
					this.Value = default;
					return;
				}

				if (type == typeof(string)) {
					value = value.ToString();
				}

				if (type.IsAssignableFrom(value.GetType())) {
					this.Value = (T)value;
					return;
				}

				if (type.IsEnum) {
					type = type.GetEnumUnderlyingType();
				}
				if (value == null) {
					this.Value = default;
				} else {
					this.Value = (T)Convert.ChangeType(value, type);
				}
			}
		}

		[SerializeField]
		private UnityEvent beforeValueChanged = new();
		public event EventHandler BeforeValueChanged;

		[SerializeField]
		private UnityEvent valueChanged = new();
		public event EventHandler ValueChanged;

		protected virtual void Start() {
			this.Invalidate();
		}

		protected virtual T CreateInstance() => Activator.CreateInstance<T>();

		private bool init;

		private Type GetElementType(IList list) {
			Queue<Type> types = new(new[] { list.GetType() });
			while (types.Count > 0) {
				Type type = types.Dequeue();
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>)) {
					return type.GetGenericArguments()[0];
				}

				foreach (Type iface in type.GetInterfaces()) {
					types.Enqueue(iface);
				}
				if (type.BaseType != null) {
					types.Enqueue(type.BaseType);
				}
			}
			return typeof(object);
		}

		public T Value {
			get {
				switch (this.valueStorageMethod) {
					case DatabindingDefaultValueStorageMethods.BindToParentMember: {
						object obj = this.ParentValue;
						if (obj == null) {
							return default;
						}
						MemberInfo member = this.Member;
						Type objType = obj?.GetType();
						if (objType != null && !member.DeclaringType.IsAssignableFrom(objType)) {
							this.member = null;
							member = this.Member;
						}
						
						if (member is MethodInfo getMethod && this.setMethod == null) {
							this.setMethod = objType.GetMethod(this.MemberName, new[] { getMethod.ReturnType }, new ParameterModifier[] { new ParameterModifier() });
							Assert.IsNotNull(this.setMethod);
						}
						object val;
						if (member == null) {
							val = obj;
						} else if (member is FieldInfo field) {
							val = field.GetValue(obj);
						} else if (member is PropertyInfo property) {
							val = property.GetValue(obj);
						} else if (member is MethodInfo method) {
							val = method.Invoke(obj, new object[] { });
						} else {
							throw new NotSupportedException();
						}

						if (val == null) {
							return default;
						}

						Type type = typeof(T);
						if (type == typeof(string)) {
							val = val.ToString();
						}

						if (type.IsAssignableFrom(val.GetType())) {
							return (T)val;
						}

						if (type.IsEnum) {
							type = type.GetEnumUnderlyingType();
						}
						return (T)Convert.ChangeType(val, type);
					}
					case DatabindingDefaultValueStorageMethods.BindToParentKey: {
						object obj = this.ParentValue;
						if (obj == null) {
							return default;
						}
						if (obj is IList list) {
							if (!int.TryParse(this.memberName, out int index)) {
								Debug.LogWarning($"{this.memberName} is not an index number.", this);
								return default;
							}
							int count = list.Count;
							if (index < 0 || index >= count) {
								Debug.LogWarning($"{index} is out of range [0, {count}).", this);
								return default;
							}
							return (T)list[index];
						} else if (obj is IDictionary dictionary) {
							ICollection collection = (ICollection)obj;

							if (!int.TryParse(this.memberName, out int index)) {
								if (!dictionary.Contains(this.memberName)) {
									Debug.LogWarning($"{this.memberName} does not exist as a key.", this);
									return default;
								}
								return (T)dictionary[this.memberName];
							}
							int count = collection.Count;
							if (index < 0 || index >= count) {
								Debug.LogWarning($"{index} is out of range [0, {count}).", this);
								return default;
							}
							return collection.Cast<T>().ElementAt(index);
						} else {
							return default;
						}
					}
					case DatabindingDefaultValueStorageMethods.New:
						if (!this.init) {
							this.value = this.CreateInstance();
						}
						break;
					case DatabindingDefaultValueStorageMethods.PlayerPrefsDataContract:
						if (!this.init) {
							this.playerPrefsKey ??= this.name;
							if (!PlayerPrefs.HasKey(this.playerPrefsKey)) {
								this.value = this.CreateInstance();
							} else {
								string json = PlayerPrefs.GetString(this.playerPrefsKey);
								DataContractJsonSerializer serializer = new(typeof(T), new DataContractJsonSerializerSettings() {
									UseSimpleDictionaryFormat = true
								});
								using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
								try {
									this.value = (T)serializer.ReadObject(stream);
								} catch (Exception ex) {
									Debug.LogError(ex);
									this.value = this.CreateInstance();
								}
							}
						}
						break;
					case DatabindingDefaultValueStorageMethods.PlayerPrefsUnityJson:
						if (!this.init) {
							this.playerPrefsKey ??= this.name;
							if (!PlayerPrefs.HasKey(this.playerPrefsKey)) {
								this.value = this.CreateInstance();
							} else {
								string json = PlayerPrefs.GetString(this.playerPrefsKey);
								try {
									this.value = JsonUtility.FromJson<T>(json);
								} catch (Exception ex) {
									Debug.LogError(ex);
									this.value = this.CreateInstance();
								}
							}
						}
						break;
				}
				this.init = true;
				return this.value;
			}
			set {
				T oldValue = this.Value;
				if ((oldValue != null && oldValue.Equals(value)) || (oldValue == null && value == null)) {
					return;
				}

				this.OnBeforeValueChanged();

				switch (this.valueStorageMethod) {
					case DatabindingDefaultValueStorageMethods.BindToParentMember: {
						MemberInfo member = this.Member ?? throw new NullReferenceException($"Member {this.memberName} not found, or parent object is null. Did you forget to change Value Storage Method from BindToParentMember?");
						object obj = this.ParentValue ?? throw new NullReferenceException();
						Type type;
						if (member is FieldInfo field) {
							type = field.FieldType;
						} else if (member is PropertyInfo property) {
							type = property.PropertyType;
						} else if (member is MethodInfo) {
							type = this.setMethod.GetParameters().First().ParameterType;
						} else {
							throw new NotSupportedException();
						}

						object newValue = value;
						if (newValue != null) {
							if (type == typeof(string)) {
								newValue = newValue.ToString();
							}

							if (!type.IsAssignableFrom(newValue.GetType())) {
								if (type.IsEnum) {
									type = type.GetEnumUnderlyingType();
								}
								try {
									newValue = Convert.ChangeType(newValue, type);
								} catch (FormatException) {
									newValue = type.IsValueType ? Activator.CreateInstance(type) : null;
								}
							}
						}

						if (member is FieldInfo field2) {
							field2.SetValue(obj, newValue);
						} else if (member is PropertyInfo property) {
							property.SetValue(obj, newValue);
						} else if (member is MethodInfo) {
							this.setMethod.Invoke(obj, new object[] { newValue });
						} else {
							throw new NotSupportedException();
						}

						if (obj.GetType().IsValueType) {
							this.Parent.Value = obj;
						}
					}
					break;
					case DatabindingDefaultValueStorageMethods.BindToParentKey: {
						object obj = this.ParentValue ?? throw new NullReferenceException();
						if (obj is not IList list) {
							if (obj is IDictionary dictionary) {
								dictionary[this.memberName] = value;
							} else {
								throw new NotSupportedException();
							}
						} else {
							if (!int.TryParse(this.memberName, out int index)) {
								Debug.LogWarning($"{this.memberName} is not an index number.", this);
								return;
							}
							int count = list.Count;
							if (index < 0 || index > count) {
								Debug.LogWarning($"{index} is out of range [0, {count}].", this);
								return;
							}

							Type type = this.GetElementType(list);
							object val = value;
							if (type == typeof(string)) {
								val = val.ToString();
							}

							if (!type.IsAssignableFrom(val.GetType())) {
								if (type.IsEnum) {
									type = type.GetEnumUnderlyingType();
								}
								val = (T)Convert.ChangeType(val, type);
							}

							list[index] = val;
						}

						if (obj.GetType().IsValueType) {
							this.Parent.Value = obj;
						}	
					}
					break;
					default:
						this.value = value;
						break;
				}

				this.OnValueChanged();

				this.Invalidate();
			}
		}

		protected virtual void OnBeforeValueChanged() {
			this.BeforeValueChanged?.Invoke(this, new EventArgs());
			this.beforeValueChanged.Invoke();
		}

		protected virtual void OnValueChanged() {
			this.ValueChanged?.Invoke(this, new EventArgs());
			this.valueChanged.Invoke();
		}

		public void Invalidate() {
			if (invalidationDefers > 0) {
				invalidationDeferred.Add(this);
				return;
			}

			this.OnInvalidate();
		}

		protected virtual void OnInvalidate() {
			this.gameObject.name = this.Value?.ToString() ?? "<Null>";
			this.Invalidated?.Invoke(this, new EventArgs());
		}
		public event EventHandler Invalidated;

		[Header("Relationships"), SerializeField]
		private GameObject parent;
		public IDatabind Parent {
			get {
				if (this.valueStorageMethod != DatabindingDefaultValueStorageMethods.BindToParentMember && this.valueStorageMethod != DatabindingDefaultValueStorageMethods.BindToParentKey) {
					return null;
				}
				if (this.parent == null) {
					IDatabind parent = this.GetComponentsInParent<IDatabind>(true).FirstOrDefault(x => x != (IDatabind)this);
#if !UNITY_EDITOR
					this.parent = ((Databind)parent)?.gameObject;
#endif
					return parent;
				}
				return this.parent.GetComponent<IDatabind>();
			}
		}
		public object ParentValue => this.Parent?.Value;

		[SerializeField, DataboundMemberName]
		private string memberName = null;
		public string MemberName { get => this.memberName; set => this.memberName = value; }
		private MemberInfo member;
		private MethodInfo setMethod;
		private MemberInfo Member {
			get {
				if (this.valueStorageMethod != DatabindingDefaultValueStorageMethods.BindToParentMember || string.IsNullOrEmpty(this.memberName)) {
					return null;
				}

				object obj = this.ParentValue;
				if (obj == null) {
					return null;
				}

				if (this.member == null) {
					Type objType = obj.GetType();
					this.member = objType.GetMember(this.MemberName).FirstOrDefault();
					if (this.member == null) {
						Debug.LogError($"{objType.Name} doesn't have member {this.MemberName}!", this);
					}
					if (this.member is MethodInfo) {
						Assert.IsNotNull(this.member = objType.GetMethod(this.MemberName, new Type[] { }, new ParameterModifier[] { }));
					}
				}
				return this.member;
			}
		}

		public Type MemberType {
			get {
				MemberInfo member = this.Member;
				if (member == null) {
					if (this.valueStorageMethod != DatabindingDefaultValueStorageMethods.BindToParentMember) {
						return this.Value?.GetType();
					}

					return null;
				} else if (member is FieldInfo field) {
					return field.FieldType;
				} else if (member is PropertyInfo property) {
					return property.PropertyType;
				} else if (member is MethodInfo method) {
					return method.ReturnType;
				} else {
					throw new NotSupportedException();
				}
			}
		}

		[SerializeField]
		protected DatabindingChangeDetectionModes changeDetectionMode;
		[SerializeField, DataboundValueChangedEvent]
		protected string valueChangedEventName = null;

		private object subscribedValue;
		private void SubscribeValueChanged() {
			object parent = this.ParentValue;
			if (parent == null || this.Member == null) {
				return;
			}
			this.subscribedValue = parent;

			Type type = parent.GetType();
			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.SubscribeToINotifyPropertyChangedEvent)) {
				if (parent is INotifyPropertyChanged notifier) {
					notifier.PropertyChanged += this.Notifier_PropertyChanged;
				} else {
					Debug.LogError($"{type.Name} does not implement INotifyPropertyChanged for monitoring {this.MemberName}!", this);
				}
			}
			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.SubscribeToOtherDotNetEvent)) {
				EventInfo eventInfo = type.GetEvent(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance);
				if (eventInfo == null) {
					Debug.LogError($"{type.Name} has no event {this.valueChangedEventName} for monitoring {this.MemberName}!", this);
				} else {
					eventInfo.AddEventHandler(parent, new EventHandler(this.Parent_EventTriggered));
				}
			}
			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.SubscribeToUnityEvent)) {
				MemberInfo member = (MemberInfo)type.GetField(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance) ??
					type.GetProperty(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance);
				if (member == null) {
					Debug.LogError($"{type.Name} has no field/property {this.valueChangedEventName} for monitoring {this.MemberName}!", this);
				} else {
					PropertyInfo property = member as PropertyInfo;
					FieldInfo field = member as FieldInfo;
					Type memberType = property?.PropertyType ?? field?.FieldType;
					if (!typeof(UnityEvent).IsAssignableFrom(memberType)) {
						Debug.LogError($"{type.Name}.{this.valueChangedEventName} is not a {nameof(UnityEvent)} and cannot be used for monitoring {this.MemberName}!", this);
					} else {
						UnityEvent unityEvent = (UnityEvent)(property?.GetValue(parent) ?? field?.GetValue(parent));
						unityEvent.AddListener(this.Invalidate);
					}
				}
			}
			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.PollEachFrame)) {
				this.lastValue = this.Value;
			}
		}
		private T lastValue;

		private void UnsubscribeValueChanged() {
			object parent = this.subscribedValue;
			if (parent == null) {
				return;
			}

			this.subscribedValue = null;

			Type type = parent.GetType();
			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.SubscribeToINotifyPropertyChangedEvent)) {
				if (parent is INotifyPropertyChanged notifier) {
					notifier.PropertyChanged -= this.Notifier_PropertyChanged;
				}
			}
			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.SubscribeToOtherDotNetEvent)) {
				EventInfo eventInfo = type.GetEvent(this.valueChangedEventName);
				eventInfo?.RemoveEventHandler(parent, new EventHandler(this.Parent_EventTriggered));
			}
			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.SubscribeToUnityEvent)) {
				MemberInfo member = (MemberInfo)type.GetField(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance) ??
					type.GetProperty(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance);
				if (member != null) {
					PropertyInfo property = member as PropertyInfo;
					FieldInfo field = member as FieldInfo;
					Type memberType = property?.PropertyType ?? field?.FieldType;
					if (typeof(UnityEvent).IsAssignableFrom(memberType)) {
						UnityEvent unityEvent = (UnityEvent)(property?.GetValue(parent) ?? field?.GetValue(parent));
						unityEvent.RemoveListener(this.Invalidate);
					}
				}
			}
			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.PollEachFrame)) {
				this.lastValue = default;
			}
		}

		private void Notifier_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == this.MemberName) {
				this.Invalidate();
			}
		}

		private void Parent_EventTriggered(object sender, EventArgs e) => this.Invalidate();

		private void Parent_BeforeValueChanged(object sender, EventArgs e) {
			this.UnsubscribeValueChanged();
		}
		private void Parent_ValueChanged(object sender, EventArgs e) {
			this.SubscribeValueChanged();
		}
		private void Parent_Invalidated(object sender, EventArgs e) {
			this.Invalidate();
		}

		private IDatabind subscribed = null;
		private void SubscribeObject() {
			if (this.valueStorageMethod != DatabindingDefaultValueStorageMethods.BindToParentMember && this.valueStorageMethod != DatabindingDefaultValueStorageMethods.BindToParentKey) {
				return;
			}

			IDatabind parent = this.Parent;
			if (this.subscribed != null || parent == null) {
				return;
			}

			parent.BeforeValueChanged += this.Parent_BeforeValueChanged;
			parent.ValueChanged += this.Parent_ValueChanged;
			parent.Invalidated += this.Parent_Invalidated;
			this.subscribed = parent;
			this.Parent_ValueChanged(parent, new EventArgs());
			this.Parent_Invalidated(parent, new EventArgs());
		}

		private void UnsubscribeObject() {
			if (this.valueStorageMethod != DatabindingDefaultValueStorageMethods.BindToParentMember && this.valueStorageMethod != DatabindingDefaultValueStorageMethods.BindToParentKey) {
				return;
			}

			IDatabind parent = this.subscribed;
			if (parent == null) {
				return;
			}

			parent.BeforeValueChanged -= this.Parent_BeforeValueChanged;
			parent.ValueChanged -= this.Parent_ValueChanged;
			parent.Invalidated -= this.Parent_Invalidated;
			this.subscribed = null;
			this.Parent_BeforeValueChanged(parent, new EventArgs());
		}

		protected virtual void OnEnable() {
			this.SubscribeObject();

			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.PollOnEnable)) {
				T val = this.Value;
				if ((this.lastValue != null && this.lastValue.Equals(val)) || (this.lastValue == null && val == null)) {
					return;
				}

				this.lastValue = val;
				this.Invalidate();
			}
		}
		protected virtual void OnDisable() {
			this.UnsubscribeObject();

			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.PollOnEnable)) {
				T val = this.Value;
				this.lastValue = val;
			}
		}

		private void Update() {
			if (this.subscribed == null && this.transform.hasChanged) {
				this.transform.hasChanged = false;
				this.SubscribeObject();
			}
			if (this.changeDetectionMode.HasFlag(DatabindingChangeDetectionModes.PollEachFrame)) {
				T val = this.Value;
				if ((this.lastValue != null && this.lastValue.Equals(val)) || (this.lastValue == null && val == null)) {
					return;
				}

				this.lastValue = val;
				this.Invalidate();
			}
		}

		public void SaveToPlayerPrefs() {
			string json;
			switch (this.valueStorageMethod) {
				case DatabindingDefaultValueStorageMethods.PlayerPrefsDataContract:
					DataContractJsonSerializer serializer = new(typeof(T), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using (MemoryStream stream = new()) {
						serializer.WriteObject(stream, this.Value);
						json = Encoding.UTF8.GetString(stream.ToArray());
					}
					break;
				case DatabindingDefaultValueStorageMethods.PlayerPrefsUnityJson:
					json = JsonUtility.ToJson(this.Value);
					break;
				default:
					throw new InvalidOperationException();
			}
			PlayerPrefs.SetString(this.playerPrefsKey, json);
		}
	}

	public enum DatabindingDefaultValueStorageMethods {
		BindToParentMember,
		BindToParentKey,
		Default,
		New,
		PlayerPrefsDataContract,
		PlayerPrefsUnityJson,
	}

	public enum DatabindingChangeDetectionModes {
		None = 0,
		SubscribeToINotifyPropertyChangedEvent = 1,
		SubscribeToOtherDotNetEvent = 2,
		SubscribeToUnityEvent = 4,
		PollEachFrame = 8,
		PollOnEnable = 16
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public class DataboundMemberNameAttribute : PropertyAttribute { }

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public class DataboundValueChangedEventAttribute : PropertyAttribute { }
}
