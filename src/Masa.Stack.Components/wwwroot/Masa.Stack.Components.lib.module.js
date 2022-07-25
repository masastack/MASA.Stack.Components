export function beforeStart(options, extensions) {
}

export function afterStarted(blazor) {
  const offset = new Date().getTimezoneOffset();
  DotNet.invokeMethodAsync("Masa.Stack.Components", "SetTimezoneOffset", offset)
}