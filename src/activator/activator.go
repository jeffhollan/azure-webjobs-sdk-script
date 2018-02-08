package main

import (
	"crypto/tls"
	"fmt"
	"log"
	"net"
	"net/http"
	"time"

	"gopkg.in/mgo.v2"
	"gopkg.in/mgo.v2/bson"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
)

func main() {
	http.HandleFunc("/", handler)
	log.Fatal(http.ListenAndServe("localhost:8000", nil))
	fmt.Println("Test")
}

func handler(w http.ResponseWriter, r *http.Request) {
	fmt.Fprintf(w, "URL.Path = %q\n", r.URL.Path)

	// TODO check on status
	checkRoute(r)

	// TODO - return the retry status

}

func checkRoute(r *http.Request) {
	dialInfo := &mgo.DialInfo{
		Addrs:    []string{"masfuncdeploy.documents.azure.com:10255"}, // Get HOST + PORT
		Timeout:  60 * time.Second,
		Database: "functions",                                                                                // It can be anything
		Username: "masfuncdeploy",                                                                            // Username
		Password: "jTK34KE4wUBYaSJ9AEVA8U1wHEtApzugYpD3tj64vxpiv1C5ZQ4IDiXSaD4dGjmQIUsDkHIYDkzG4THIwxjEUg==", // PASSWORD
		DialServer: func(addr *mgo.ServerAddr) (net.Conn, error) {
			return tls.Dial("tcp", addr.String(), &tls.Config{})
		},
	}
	// https://github.com/Azure-Samples/azure-cosmos-db-mongodb-golang-getting-started
	session, err := mgo.DialWithInfo(dialInfo)
	if err != nil {
		log.Printf("Could not connect")
		log.Fatal(err)
	}

	// TEMP - print out the database and collection names
	databaseNames, _ := session.DatabaseNames()
	for _, databaseName := range databaseNames {
		log.Printf("Have database %s\n", databaseName)

		database := session.DB(databaseName)
		collectionNames, _ := database.CollectionNames()
		for _, collectionName := range collectionNames {
			log.Printf("Have collection %s\n", collectionName)

			c := session.DB(databaseName).C(collectionName)
			count, _ := c.Count()
			log.Printf("%d documents in collection", count)

			var docs []bson.M
			err = c.Find(nil).All(&docs)
			if err != nil {
				log.Printf("Could not collect all documents")
			}
			for _, doc := range docs {
				log.Printf("Found a doc! %s\n", doc)
			}

			//c.Find(nil).All()
		}
	}
	collection := session.DB("functions").C("routes")

	// TODO - fix this bug (why 8is)
	var routes []Route
	query := bson.M{"host": r.Host}
	log.Printf("Querying for %s\n", query)

	err = collection.Find(query).All(&routes)
	if err != nil {
		log.Printf("Test")
	}
	log.Printf("Result is %s\n", routes)

	result := Route{}
	if result.host == "" {
		log.Printf("No function host registered for %s", r.Host)
	} else {
		log.Printf("Function host registered for %s", result.host)
		log.Printf("Script path is %s\n", result.scripts)

		deployment := getDeploymentDefinition(&result)
		log.Println(deployment)
	}
}

func deployFunctionsHost(route *Route) {
	log.Printf("Deploying function host for %s\n", route.host)

	// TODO - create a custom deployment definition
	// https://github.com/kubernetes/client-go/blob/master/examples/create-update-delete-deployment/main.go

	// TODO - create an ingress definition
	// TODO - deploy
	// TODO - implement directly, not with shell out to kubectl

	// TODO - wait for this to come up successfully

}

func waitForReady(route *Route) {

}

func checkStatusOutCluster(route *Route) {

}

func checkStatusInCluster(route *Route) {
	// Create the in-cluster config
	config, err := rest.InClusterConfig()
	if err != nil {
		panic(err.Error())
	}

	// Create the clientset
	clientset, err := kubernetes.NewForConfig(config)
	if err != nil {
		panic(err.Error())
	}

	// _, err = clientset.CoreV1().Pods()
	_, err = clientset.CoreV1().Pods("default").Get("example-xxxxx", metav1.GetOptions{})

}

type Route struct {
	host    string
	scripts string
	//deploymentName string
}
