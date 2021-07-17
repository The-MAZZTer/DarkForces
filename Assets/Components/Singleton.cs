using System;
using UnityEngine;

namespace MZZT {
	public enum SingletonDuplicateActions {
		Nothing,
		ThrowException,
		DestroySelf,
		ReplaceInstance
	}

	public class Singleton<T> : MonoBehaviour where T : Singleton<T> {
		//public static SingletonDuplicateActions DuplicateAction { get; set; } = SingletonDuplicateActions.DestroySelf;
		//public static bool PersistAcrossScenes { get; set; } = false;

		[SerializeField, Header("Singleton")]
		protected SingletonDuplicateActions duplicateAction = SingletonDuplicateActions.DestroySelf;
		[SerializeField]
		protected bool persistAcrossScenes = false;

		public static T Instance { get; private set; }

		protected void OnDestroyAsDuplicate() {
			if (this.persistAcrossScenes) {
				DestroyImmediate(this.gameObject);
			} else {
				DestroyImmediate(this);
			}
		}

		protected virtual void Awake() {
			if (Instance != null) {
				switch (this.duplicateAction) {
					case SingletonDuplicateActions.Nothing:
						return;
					case SingletonDuplicateActions.ThrowException:
						throw new InvalidOperationException(
							$"{this.gameObject.name}: There already exists an object of singleton {typeof(T).Name}.");
					case SingletonDuplicateActions.ReplaceInstance:
						Instance.OnDestroyAsDuplicate();
						break;
					default:
						this.OnDestroyAsDuplicate();
						return;
				}
			}
			Instance = (T)this;

			if (this.persistAcrossScenes) {
				this.transform.SetParent(null, true);
				DontDestroyOnLoad(this.gameObject);
			}
		}

		protected virtual void OnDestroy() {
			if (Instance == this) {
				Instance = null;
			}
		}
	}
}
