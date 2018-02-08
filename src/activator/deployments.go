package main

import (
	appsv1beta1 "k8s.io/api/apps/v1beta1"
	apiv1 "k8s.io/api/core/v1"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
)

func getDeploymentDefinition(r *Route) *appsv1beta1.Deployment {
	// TODO - create a custom deployment definition
	// https://github.com/kubernetes/client-go/blob/master/examples/create-update-delete-deployment/main.go
	var replicaCount int32 = 1

	deployment := appsv1beta1.Deployment{
		ObjectMeta: metav1.ObjectMeta{
			Name: "test",
		},
		Spec: appsv1beta1.DeploymentSpec{
			Replicas: &replicaCount,
			Template: apiv1.PodTemplateSpec{
				ObjectMeta: metav1.ObjectMeta{
					Labels: map[string]string{
						"app": "TODO",
					},
				},
				Spec: apiv1.PodSpec{
					Containers: []apiv1.Container{
						{
							Name:  "functions-worker-host",
							Image: "mabsimms/k8host",
							Ports: []apiv1.ContainerPort{
								{
									Name:          "http",
									Protocol:      apiv1.ProtocolTCP,
									ContainerPort: 80,
								},
								{
									Name:          "http",
									Protocol:      apiv1.ProtocolTCP,
									ContainerPort: 443,
								},
							},
							Env: []apiv1.EnvVar{
								{
									Name:  "FUNCTION_DEPLOYMENT",
									Value: "TODO",
								},
								{
									Name:  "FUNCTIONS_K8CONFIG",
									Value: "/config/appsettings.json",
								},
								{
									Name: "FUNCTION_POD_NAME",
									ValueFrom: &apiv1.EnvVarSource{
										FieldRef: &apiv1.ObjectFieldSelector{
											FieldPath: "metadata.name",
										},
									},
								},
								{
									Name: "FUNCTION_NODE_NAME",
									ValueFrom: &apiv1.EnvVarSource{
										FieldRef: &apiv1.ObjectFieldSelector{
											FieldPath: "spec.nodeName",
										},
									},
								},
								{
									Name: "FUNCTION_NAMESPACE_NAME",
									ValueFrom: &apiv1.EnvVarSource{
										FieldRef: &apiv1.ObjectFieldSelector{
											FieldPath: "metadata.namespace",
										},
									},
								},
								{
									Name: "FUNCTION_POD_IP",
									ValueFrom: &apiv1.EnvVarSource{
										FieldRef: &apiv1.ObjectFieldSelector{
											FieldPath: "status.podIP",
										},
									},
								},
							},
							VolumeMounts: []apiv1.VolumeMount{
								{
									MountPath: "/config",
									Name:      "config-volume",
									ReadOnly:  true,
								},
								{
									MountPath: "/scripts",
									Name:      "scripts-local-volume",
									ReadOnly:  false,
								},
								{
									MountPath: "/scripts-remote",
									Name:      "scripts-azurefiles-volume",
									ReadOnly:  true,
								},
								{
									MountPath: "/logs",
									Name:      "logs-volume",
									ReadOnly:  false,
								},
							},
						},
					},
					Volumes: []apiv1.Volume{
						apiv1.Volume{
							Name: "config-volume",
							VolumeSource: apiv1.VolumeSource{
								ConfigMap: &apiv1.ConfigMapVolumeSource{
									LocalObjectReference: apiv1.LocalObjectReference{
										Name: "functions-config",
									},
								},
							},
						},
						apiv1.Volume{
							Name: "logs-volume",
							VolumeSource: apiv1.VolumeSource{
								EmptyDir: &apiv1.EmptyDirVolumeSource{},
							},
						},
						apiv1.Volume{
							Name: "scripts-local-volume",
							VolumeSource: apiv1.VolumeSource{
								EmptyDir: &apiv1.EmptyDirVolumeSource{},
							},
						},
						apiv1.Volume{
							Name: "scripts-remote-volume",
							VolumeSource: apiv1.VolumeSource{
								AzureFile: &apiv1.AzureFileVolumeSource{
									SecretName: "script-azure-file",
									ShareName:  "scriptshare",
								},
							},
						},
					},
				},
			},
		},
	}
	return &deployment
}
