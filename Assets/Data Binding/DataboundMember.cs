using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;
using Component = UnityEngine.Component;

namespace MZZT.DataBinding {
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public class DataboundMemberNameAttribute : PropertyAttribute { }

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public class DataboundMemberEventAttribute : PropertyAttribute { }

	public abstract class DataboundMember : MonoBehaviour {
		private IDataboundObject databoundObject;
		protected IDataboundObject DataboundObject {
			get {
				if (this.databoundObject == null) {
					this.databoundObject = this.GetComponentsInParent<IDataboundObject>(true)
						.First(x => ((Component)x).gameObject != this.gameObject);
				}
				return this.databoundObject;
			}
		}

		[SerializeField, DataboundMemberName]
		protected string memberName = null;
		public string MemberName => this.memberName;

		private MemberInfo member;
		protected MemberInfo Member {
			get {
				if (string.IsNullOrEmpty(this.memberName)) {
					return null;
				}

				if (this.member == null) {
					Type objType = this.DataboundObject.GetType().GetProperty(nameof(IDataboundObject.Value)).PropertyType;
					this.member = objType.GetMember(this.MemberName).FirstOrDefault();
					if (this.member == null) {
						Debug.LogError($"{objType.Name} doesn't have member {this.MemberName}!");
					}
					if (this.member is MethodInfo) {
						Assert.IsNotNull(this.member = objType.GetMethod(this.MemberName, new Type[] { }, new ParameterModifier[] { }));
					}
				}
				return this.member;
			}
		}

		protected Type MemberType {
			get {
				if (this.Member == null) {
					return this.DataboundObject.GetType();
				} else if (this.Member is FieldInfo field) {
					return field.FieldType;
				} else if (this.Member is PropertyInfo property) {
					return property.PropertyType;
				} else if (this.Member is MethodInfo method) {
					return method.ReturnType;
				} else {
					throw new NotSupportedException();
				}
			}
		}
	}

	public abstract class DataboundMember<T> : DataboundMember {
		private MethodInfo setMethod;
		public T Value {
			get {
				Type objType = this.DataboundObject.GetType().GetProperty(nameof(IDataboundObject.Value)).PropertyType;
				if (this.Member is MethodInfo && this.setMethod == null) {
					this.setMethod = objType.GetMethod(this.MemberName, new[] { (this.Member as MethodInfo).ReturnType }, new ParameterModifier[] { new ParameterModifier() });
					Assert.IsNotNull(this.setMethod);
				}
				object obj = this.DataboundObject.Value;
				if (obj == null) {
					return default;
				}
				object val;
				if (this.Member == null) {
					val = obj;
				} else if (this.Member is FieldInfo field) {
					val = field.GetValue(obj);
				} else if (this.Member is PropertyInfo property) {
					val = property.GetValue(obj);
				} else if (this.Member is MethodInfo method) {
					val = method.Invoke(obj, new object[] { });
				} else {
					throw new NotSupportedException();
				}
				if (typeof(T) == typeof(string) && val != null) {
					val = val.ToString();
				}
				return (T)Convert.ChangeType(val, typeof(T));
			}
			set {
				this.BeginUpdate();
				try {
					T oldValue = this.Value;
					if ((oldValue != null && oldValue.Equals(value)) || (oldValue == null && value == null)) {
						return;
					}

					if (this.Member == null) {
						this.DataboundObject.Value = value;
						this.Invalidate();
						return;
					}

					object obj = this.DataboundObject.Value;
					if (obj == null) {
						throw new NullReferenceException();
					}

					Type type;
					if (this.Member is FieldInfo field) {
						type = field.FieldType;
					} else if (this.Member is PropertyInfo property) {
						type = property.PropertyType;
					} else if (this.Member is MethodInfo) {
						type = this.setMethod.GetParameters().First().ParameterType;
					} else {
						throw new NotSupportedException();
					}

					if (type.IsEnum) {
						type = type.GetEnumUnderlyingType();
					}
					object newValue = Convert.ChangeType(value, type);

					if (this.Member is FieldInfo field2) {
						field2.SetValue(obj, newValue);
					} else if (this.Member is PropertyInfo property) {
						property.SetValue(obj, newValue);
					} else if (this.Member is MethodInfo) {
						this.setMethod.Invoke(obj, new object[] { newValue });
					} else {
						throw new NotSupportedException();
					}

					this.Invalidate();
				} finally {
					this.EndUpdate();
				}
			}
		}

		private bool pendingInvalidate = false;
		private int updateDeferLevel = 0;
		public void BeginUpdate() {
			this.updateDeferLevel++;
		}

		public void EndUpdate() {
			this.updateDeferLevel--;
			Assert.IsTrue(this.updateDeferLevel >= 0);
			if (this.updateDeferLevel == 0 && this.pendingInvalidate) {
				this.Invalidate();
			}
		}

		public void Invalidate() {
			if (this.updateDeferLevel > 0) {
				this.pendingInvalidate = true;
				return;
			}

			this.pendingInvalidate = false;
			this.OnInvalidate();
		}
		protected abstract void OnInvalidate();

		[SerializeField]
		protected ChangeListenModes changeListenMode = ChangeListenModes.SubscribeToINotifyPropertyChangedEvent;
		[SerializeField, DataboundMemberEvent]
		protected string valueChangedEventName = null;

		private void SubscribeValueChanged() {
			if (this.DataboundObject == null || this.DataboundObject.Value == null || this.Member == null) {
				return;
			}

			if (this.changeListenMode.HasFlag(ChangeListenModes.SubscribeToINotifyPropertyChangedEvent)) {
				if (this.DataboundObject.Value is INotifyPropertyChanged notifier) {
					notifier.PropertyChanged += this.Notifier_PropertyChanged;
				} else {
					Debug.LogError($"{this.DataboundObject.Value.GetType().Name} does not implement INotifyPropertyChanged for monitoring {this.MemberName}!");
				}
			}
			if (this.changeListenMode.HasFlag(ChangeListenModes.SubscribeToOtherDotNetEvent)) {
				Type type = this.DataboundObject.Value.GetType();
				EventInfo eventInfo = type.GetEvent(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance);
				if (eventInfo == null) {
					Debug.LogError($"{this.DataboundObject.Value.GetType().Name} has no event {this.valueChangedEventName} for monitoring {this.MemberName}!");
				} else {
					eventInfo.AddEventHandler(this, new EventHandler(this.ValueChanged));
				}
			}
			if (this.changeListenMode.HasFlag(ChangeListenModes.SubscribeToUnityEvent)) {
				Type type = this.DataboundObject.Value.GetType();
				MemberInfo member = (MemberInfo)type.GetField(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance) ??
					type.GetProperty(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance);
				if (member == null) {
					Debug.LogError($"{this.DataboundObject.Value.GetType().Name} has no field/property {this.valueChangedEventName} for monitoring {this.MemberName}!");
				} else {
					PropertyInfo property = member as PropertyInfo;
					FieldInfo field = member as FieldInfo;
					Type memberType = property?.PropertyType ?? field?.FieldType;
					if (!typeof(UnityEvent).IsAssignableFrom(memberType)) {
						Debug.LogError($"{this.DataboundObject.Value.GetType().Name}.{this.valueChangedEventName} is not a {nameof(UnityEvent)} and cannot be used for monitoring {this.MemberName}!");
					} else {
						UnityEvent unityEvent = (UnityEvent)(property?.GetValue(this.DataboundObject.Value) ?? field?.GetValue(this.DataboundObject.Value));
						unityEvent.AddListener(this.Invalidate);
					}
				}
			}
			if (this.changeListenMode.HasFlag(ChangeListenModes.PollEachFrame)) {
				this.lastValue = this.Value;
			}
		}
		private object lastValue;

		private void UnsubscribeValueChanged() {
			if (this.DataboundObject == null || this.DataboundObject.Value == null || this.Member == null) {
				return;
			}

			if (this.changeListenMode.HasFlag(ChangeListenModes.SubscribeToINotifyPropertyChangedEvent)) {
				if (this.DataboundObject.Value is INotifyPropertyChanged notifier) {
					notifier.PropertyChanged -= this.Notifier_PropertyChanged;
				}
			}
			if (this.changeListenMode.HasFlag(ChangeListenModes.SubscribeToOtherDotNetEvent)) {
				Type type = this.DataboundObject.Value.GetType();
				EventInfo eventInfo = type.GetEvent(this.valueChangedEventName);
				eventInfo?.RemoveEventHandler(this, new EventHandler(this.ValueChanged));
			}
			if (this.changeListenMode.HasFlag(ChangeListenModes.SubscribeToUnityEvent)) {
				Type type = this.DataboundObject.Value.GetType();
				MemberInfo member = (MemberInfo)type.GetField(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance) ??
					type.GetProperty(this.valueChangedEventName, BindingFlags.Public | BindingFlags.Instance);
				if (member != null) {
					PropertyInfo property = member as PropertyInfo;
					FieldInfo field = member as FieldInfo;
					Type memberType = property?.PropertyType ?? field?.FieldType;
					if (typeof(UnityEvent).IsAssignableFrom(memberType)) {
						UnityEvent unityEvent = (UnityEvent)(property?.GetValue(this.DataboundObject.Value) ?? field?.GetValue(this.DataboundObject.Value));
						unityEvent.RemoveListener(this.Invalidate);
					}
				}
			}
			if (this.changeListenMode.HasFlag(ChangeListenModes.PollEachFrame)) {
				this.lastValue = null;
			}
		}

		private void Notifier_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == this.MemberName) {
				this.Invalidate();
			}
		}

		private void ValueChanged(object sender, EventArgs e) => this.Invalidate();

		private void DataboundObject_BeforeValueChanged(object sender, EventArgs e) {
			this.UnsubscribeValueChanged();
		}
		private void DataboundObject_ValueChanged(object sender, EventArgs e) {
			this.SubscribeValueChanged();
			this.Invalidate();
		}

		private bool subscribed = false;
		private void SubscribeObject() {
			if (this.subscribed || this.DataboundObject == null) {
				return;
			}

			this.DataboundObject.BeforeValueChanged += this.DataboundObject_BeforeValueChanged;
			this.DataboundObject.ValueChanged += this.DataboundObject_ValueChanged;
			this.DataboundObject_ValueChanged(this.DataboundObject, new EventArgs());
			this.subscribed = true;
		}
		private void UnsubscribeObject() {
			if (!this.subscribed) {
				return;
			}

			this.DataboundObject.BeforeValueChanged -= this.DataboundObject_BeforeValueChanged;
			this.DataboundObject.ValueChanged -= this.DataboundObject_ValueChanged;
			this.DataboundObject_BeforeValueChanged(this.DataboundObject, new EventArgs());
			this.subscribed = false;
		}

		protected virtual void OnEnable() {
			this.SubscribeObject();

			if (this.changeListenMode.HasFlag(ChangeListenModes.PollOnEnable)) {
				object val = this.Value;
				if (val != this.lastValue) {
					this.lastValue = val;
					this.Invalidate();
				}
			}
		}
		//protected virtual void OnDisable() => this.UnsubscribeObject();

		private void Update() {
			if (!this.subscribed && this.transform.hasChanged) {
				this.transform.hasChanged = false;
				this.SubscribeObject();
			}
			if (this.changeListenMode.HasFlag(ChangeListenModes.PollEachFrame)) {
				object val = this.Value;
				if (val != this.lastValue) {
					this.lastValue = val;
					this.Invalidate();
				}
			}
		}
	}

	public enum ChangeListenModes {
		None = 0,
		SubscribeToINotifyPropertyChangedEvent = 1,
		SubscribeToOtherDotNetEvent = 2,
		SubscribeToUnityEvent = 4,
		PollEachFrame = 8,
		PollOnEnable = 16
	}

	public interface IDataboundUi {
		bool IsDirty { get; }
		void Apply();
		void BeginUpdate();
		void EndUpdate();
		void Invalidate();
		void Revert();
		event EventHandler IsDirtyChanged;
	}

	public abstract class DataboundUi<T> : DataboundMember<T>, IDataboundUi {
		private Selectable selectable;
		protected Selectable Selectable {
			get {
				if (this.selectable == null) {
					this.selectable = this.GetComponent<Selectable>();
				}
				return this.selectable;
			}
		}

		private bool CheckIsDirty() {
			T value = this.Value;
			T user = this.UserEnteredValue;
			return (value == null || !value.Equals(user)) && (value != null || user != null);
		}

		private bool isDirty;
		public bool IsDirty {
			get => this.isDirty;
			private set {
				if (this.isDirty == value) {
					return;
				}
				this.isDirty = value;

				this.IsDirtyChanged?.Invoke(this, new EventArgs());
			}
		}
		public event EventHandler IsDirtyChanged;

		protected virtual bool IsReadOnly => this.DataboundObject.Value == null || !this.Selectable.interactable;

		public event EventHandler UserEnteredValueChanged;
		protected void OnUserEnteredValueChanged() {
			this.IsDirty = this.CheckIsDirty();

			this.UserEnteredValueChanged?.Invoke(this, new EventArgs());

			if (!this.AutoApplyOnChange || this.IsReadOnly || !this.IsDirty) {
				return;
			}

			this.Apply();
		}

		[SerializeField]
		private bool autoApplyOnChange;
		public bool AutoApplyOnChange { get => this.autoApplyOnChange; set => this.autoApplyOnChange = value; }

		public void Apply() {
			if (!this.IsDirty) {
				return;
			}

			this.Value = this.UserEnteredValue;
			this.IsDirty = false;
			this.DataboundObject.Invalidate();
		}

		public void Revert() {
			if (!this.IsDirty) {
				return;
			}

			this.UserEnteredValue = this.Value;
			this.IsDirty = false;
		}

		protected abstract T UserEnteredValue { get; set; }

		protected override void OnInvalidate() {
			if (this.IsDirty) {
				return;
			}

			if (this.DataboundObject == null || this.DataboundObject.Value == null) {
				return;
			}

			this.UserEnteredValue = this.Value;
		}
	}
}
