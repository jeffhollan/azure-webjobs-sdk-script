package main

import (
	"flag"
	"log"
	"os"
	"path/filepath"

	"k8s.io/apimachinery/pkg/util/intstr"

	"k8s.io/client-go/tools/clientcmd"

	appsv1beta1 "k8s.io/api/apps/v1beta1"
	apiv1 "k8s.io/api/core/v1"
	extensionsv1beta1 "k8s.io/api/extensions/v1beta1"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/util/homedir"
)

func deployFunctionsHost(route *route, client *kubernetes.Clientset) {
	log.Printf("Deploying function host for %s\n", route.Host)

	// TODO - create a custom deployment definition
	// TODO - create an ingress definition
	// https://github.com/kubernetes/client-go/blob/master/examples/create-update-delete-deployment/main.go
	// https://github.com/timoreimann/kubernetes-goclient-example/blob/master/operation.go
	deploymentDefinition := getDeploymentDefinition(route)
	log.Println(deploymentDefinition)

	// Deploy the function host
	deploymentsClient := client.AppsV1beta1().Deployments(apiv1.NamespaceDefault)

	deployment, err := deploymentsClient.Create(deploymentDefinition)
	if err != nil {
		log.Printf("Could not execute deployment: %s\n", err)
	} else {
		log.Printf("deployment name is %s\n", deployment.GetName())
	}

	servicesClient := client.Core().Services(apiv1.NamespaceDefault)
	serviceDefinition := getServiceDefinition(route)
	service, err := servicesClient.Create(serviceDefinition)
	if err != nil {
		log.Printf("Could not execute deployment: %s\n", err)
	} else {
		log.Printf("deployment name is %s\n", service.GetName())
	}

	ingressClient := client.ExtensionsV1beta1().Ingresses(apiv1.NamespaceDefault)
	ingressDefinition := getIngressDefinition(route)
	ingress, err := ingressClient.Create(ingressDefinition)
	if err != nil {
		log.Printf("Could not execute deployment: %s\n", err)
	} else {
		log.Printf("deployment name is %s\n", ingress.GetName())
	}

	// Wait for the deployment to come up successfully
	// TODO
}

func getClusterClient() (*kubernetes.Clientset, error) {
	if _, err := os.Stat("/etc/labels"); err == nil {
		// /etc/labels exists; running inside a K8 cluster
		config, err := rest.InClusterConfig()
		if err != nil {
			return nil, err
		}
		clientset, err := kubernetes.NewForConfig(config)
		if err != nil {
			return nil, err
		}
		return clientset, nil
	} else {
		// Running directly; leverage user context kube config
		var kubeConfigPath string
		if os.Getenv("KUBECONFIG") != "" {
			kubeConfigPath = os.Getenv("KUBECONFIG")
		} else if home := homedir.HomeDir(); home != "" {
			kubeConfigPath = filepath.Join(home, ".kube", "config")
		} else {
			log.Printf("Could not find home directory or $KUBECONFIG setting\n")
			return nil, err
		}

		if _, err := os.Stat(kubeConfigPath); os.IsNotExist(err) {
			// kube config file does not exist
			log.Printf("Could not find kube configuration file %s\n", kubeConfigPath)
			return nil, err
		}

		var kubeFlags *string
		kubeFlags = flag.String("kubeconfig", kubeConfigPath, "")
		flag.Parse()

		config, err := clientcmd.BuildConfigFromFlags("", *kubeFlags)
		clientset, err := kubernetes.NewForConfig(config)
		if err != nil {
			return nil, err
		}
		return clientset, nil
	}
}

func checkStatusInCluster(route *route) {

	// _, err = clientset.CoreV1().Pods()
	//_, err = clientset.CoreV1().Pods("default").Get("example-xxxxx", metav1.GetOptions{})
}

// func getClientset() (*apiv1.Clientset, error) {

// }

func getServiceDefinition(r *route) *apiv1.Service {
	service := apiv1.Service{
		ObjectMeta: metav1.ObjectMeta{
			Name: r.DeploymentName,
		},
		Spec: apiv1.ServiceSpec{
			Ports: []apiv1.ServicePort{
				{
					Name:       "http",
					TargetPort: intstr.FromInt(80),
					Port:       80,
				},
			},
			Selector: map[string]string{
				"app":  "cheese",
				"task": "stilton",
			},
		},
	}

	return &service
}

func getIngressDefinition(r *route) *extensionsv1beta1.Ingress {
	ingress := extensionsv1beta1.Ingress{
		ObjectMeta: metav1.ObjectMeta{
			Name: r.DeploymentName,
			Annotations: map[string]string{
				"kubernetes.io/ingress.class": "traefik",
			},
		},
		Spec: extensionsv1beta1.IngressSpec{
			Rules: []extensionsv1beta1.IngressRule{
				{
					r.Host,
					extensionsv1beta1.IngressRuleValue{
						&extensionsv1beta1.HTTPIngressRuleValue{
							Paths: []extensionsv1beta1.HTTPIngressPath{
								{
									Path: "/",
									Backend: extensionsv1beta1.IngressBackend{
										ServiceName: r.DeploymentName,
										ServicePort: intstr.FromString("http"),
									},
								},
							},
						},
					},
				},
			},
		},
	}
	return &ingress
}

func getDeploymentDefinition(r *route) *appsv1beta1.Deployment {
	// TODO - create a custom deployment definition
	// https://github.com/kubernetes/client-go/blob/master/examples/create-update-delete-deployment/main.go
	var replicaCount int32 = 1

	deployment := appsv1beta1.Deployment{
		ObjectMeta: metav1.ObjectMeta{
			Name: r.DeploymentName,
		},
		Spec: appsv1beta1.DeploymentSpec{
			Replicas: &replicaCount,
			Template: apiv1.PodTemplateSpec{
				ObjectMeta: metav1.ObjectMeta{
					Labels: map[string]string{
						"app": r.DeploymentName + "-functions-worker",
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
									Name:          "https",
									Protocol:      apiv1.ProtocolTCP,
									ContainerPort: 443,
								},
							},
							Env: []apiv1.EnvVar{
								{
									Name:  "FUNCTION_DEPLOYMENT",
									Value: r.DeploymentName,
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
									Name:      "scripts-remote-volume",
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
