package main

import (
	"log"
	"net/http"
)

var users = make(map[int]User)

func main() {
	users[0] = User{}
	mux := http.NewServeMux()
	mux.HandleFunc("/user/", http.HandlerFunc(showUserPage))
	err := http.ListenAndServe(":80", mux)
	log.Fatal(err)
}
