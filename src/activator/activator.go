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
)

func main() {
	http.HandleFunc("/", handler)
	log.Fatal(http.ListenAndServe("localhost:8000", nil))
	fmt.Println("Test")
}

func handler(w http.ResponseWriter, r *http.Request) {
	fmt.Fprintf(w, "URL.Path = %q\n", r.URL.Path)

	// TODO check on status
	checkRoute(r, w)

	// TODO - return the retry status
}

func checkRoute(r *http.Request, w http.ResponseWriter) {
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
	collection := session.DB("functions").C("routes")

	query := bson.M{"host": r.Host}
	log.Printf("Querying for %s\n", query)

	var result route
	err = collection.Find(query).One(&result)

	if result.Host == "" {
		log.Printf("No function host registered for %s", r.Host)
		return
	}

	log.Printf("Function host registered for %s", result.Host)
	log.Printf("Script path is %s\n", result.Scripts)

	// TODO - fix this
	result.DeploymentName = "test"

	client, err := getClusterClient()
	if err != nil {
		log.Printf("Could not obtain cluster client for %s", err)
		return
	}

	deployFunctionsHost(&result, client)

	// Redirect the HTTP call to the new service
	// TODO
}

type route struct {
	Id             bson.ObjectId `bson:"_id,omitempty"`
	Host           string
	Scripts        string
	DeploymentName string
}
