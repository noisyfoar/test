package main

import (
	"encoding/json"
	"fmt"
	"net/http"
	"path"
	"strconv"
)

type User struct {
	ID    int    `json:"ID"`
	Token string `json:"Token"`
	Name  string `json:"Name"`
	Age   int    `json:"Age"`
}

func showUserPage(writer http.ResponseWriter, request *http.Request) {

	id, err := strconv.Atoi(path.Base(request.URL.String()))

	if request.Method == http.MethodPost {

		temp_user := User{}
		err := json.NewDecoder(request.Body).Decode(&temp_user)

		if err != nil {
			fmt.Println(err)
			return
		}
		_, check := users[id]
		if check == true {
			users[id] = User{ID: id, Token: users[id].Token, Name: temp_user.Name, Age: temp_user.Age}
			temp_user = users[id]
			writer.Write([]byte("Successfully changed\n"))
		} else {
			addUser(temp_user, id)
			writer.Write([]byte("Successfully added\n"))
		}

		data := []byte(strconv.Itoa(id) + " " + temp_user.Name + " " + temp_user.Token + " " + strconv.Itoa(users[id].Age) + "\n")
		writer.Write(data)
	}
	if request.Method == http.MethodGet {

		if err != nil {
			fmt.Println(err)
			return
		}

		_, check := users[id]

		if !check || id < 1 {
			writer.Write([]byte("not existing\n"))
			return
		}
		temp_user, err := json.Marshal(users[id])

		if err != nil {
			fmt.Println(err)
			return
		}
		writer.Write(temp_user)
	}
}

// takes user with id, name and age and add to "database"
func addUser(user User, ID int) {
	user.ID = ID
	user.Token = createToken(user)
	users[ID] = user
}

// takes user but changes only name and age and change his model in "database"
func changeUser(user User) {
	var id = user.ID
	{
		var token = users[id].Token
		users[id] = User{ID: id, Token: token, Name: user.Name, Age: user.Age}
	}
}

// takes user with id, name and age
func createToken(user User) string {
	return "token"
}
