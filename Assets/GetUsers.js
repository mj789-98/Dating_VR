handlers.getUsers = function (args, context) {
  // Get all users (you might need to adjust this based on your PlayFab setup)
  var users = server.GetAllUsers({}).Data.Users;

  var usernames = [];
  for (var i = 0; i < users.length; i++) {
    usernames.push(users[i].TitleDisplayName); // or any other relevant property
  }

  return { usernames: usernames };
};
