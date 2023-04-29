import socket
import json

def server_program():
    # get the hostname
    host = "localhost"
    port = 33333  # initiate port no above 1024

    server_socket = socket.socket()  # get instance
    # look closely. The bind() function takes tuple as argument
    server_socket.bind((host, port))  # bind host address and port together

    # configure how many client the server can listen simultaneously
    server_socket.listen(2)
    conn, address = server_socket.accept()  # accept new connection
    print("Connection from: " + str(address))
    while True:
        # receive data stream. it won't accept data packet greater than 1024 bytes
        data = conn.recv(4096).decode()
        if not data:
            # if data is not received break
            break

        # Ignore BM responses
        for d in (str(data)).split("\0"):
            if (len(d) == 0):
                continue
            print(d)
            js = json.loads(d)
            if ('ok' in js):
                continue

            # Mirror other data
            # data = input(' -> ')
            conn.send(d.encode())  # send data to the client

    conn.close()  # close the connection


if __name__ == '__main__':
    server_program()