mergeInto(LibraryManager.library, {
	OnApiCallFinished: function(id) { window.__levelPreview.__onApiCallFinished(id); },
	OnEvent0Args: function(eventName) { window.__levelPreview.__onEvent(UTF8ToString(eventName)); },
	OnEvent1Args: function(eventName, arg0) { window.__levelPreview.__onEvent(UTF8ToString(eventName), UTF8ToString(arg0)); },
	OnEvent2Args: function(eventName, arg0, arg1) { window.__levelPreview.__onEvent(UTF8ToString(eventName), UTF8ToString(arg0), UTF8ToString(arg1)); },
	OnEvent3Args: function(eventName, arg0, arg1, arg2) { window.__levelPreview.__onEvent(UTF8ToString(eventName), UTF8ToString(arg0), UTF8ToString(arg1), UTF8ToString(arg2)); }
});
