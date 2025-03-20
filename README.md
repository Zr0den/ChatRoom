## Features

- Channel Creation: Users can create new chat channels.
- Invite Keys: When a channel is created, an invite key is generated, which can be shared with others to join the channel securely.
- Confidentiality and Integrity: All communications are encrypted using built-in C# cryptographic libraries to ensure data integrity and confidentiality.
- Join Channels: Users can join existing channels by entering the correct invite key.

## Technologies Used

- C#: The primary programming language.
- .NET Framework: Utilized for built-in cryptographic functionalities.
- Console Application: The user interface is simple and text-based, operating in a console environment.

## Installation

1. Clone this repository:
    ```bash
    git clone https://github.com/Zr0den/ChatRoom.git
    ```

2. Open the solution in Visual Studio

3. Build and run the ChatRoomServer

4. Run Clients w/o debug for multiple instances

## Usage

- To create a channel, run the application and follow the prompts to create a new channel. A unique invite key will be generated for your channel.
- To join a channel, enter the correct invite key when prompted.
- The chat will be encrypted, ensuring that the messages are confidential and the integrity of the data is maintained.
  
## Security Features

- **AES Encryption** Messages are encrypted using AES (Advanced Encryption Standard) to ensure confidentiality.
- **Hashing** Messages are hashed to verify their integrity and authenticity.



Needs identification / joined notice.