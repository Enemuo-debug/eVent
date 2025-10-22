document.getElementById("loginForm").addEventListener("submit", async (event) => {
  event.preventDefault();

  const messageEl = document.getElementById("message");
  messageEl.textContent = "Logging in...";
  messageEl.style.color = "#444";

  const loginData = {
    UserName: document.getElementById("username").value.trim(),
    Password: document.getElementById("password").value.trim(),
  };

  try {
    const response = await fetch("http://localhost:5008/managers/login", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      // If you're using JWT Cookie Auth, set credentials to "include"
      credentials: "include", 
      body: JSON.stringify(loginData),
    });

    if (response.ok) {
      messageEl.textContent = response.message || "Login successful!";
      messageEl.style.color = "green";
      // Redirect after login
      setTimeout(() => window.location.href = "dashboard.html", 1500);
    } else {
      messageEl.textContent = response.message || "Invalid login credentials.";
      messageEl.style.color = "red";
    }

  } catch (error) {
    console.error("Error:", error);
    messageEl.textContent = "Server error. Please try again.";
    messageEl.style.color = "red";
  }
});
