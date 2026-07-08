{{/*
Common labels applied to every resource.
*/}}
{{- define "autopilot.labels" -}}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/part-of: autopilot
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
{{- end -}}
