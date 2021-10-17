import time
import uuid
import random
from locust import HttpUser, task, between


class MatchmakingUser(HttpUser):
    wait_time = between(1, 5)
    guid = ""

    def matchmaking_join(self):
        self.client.post("http://127.0.0.1:90/v1/matchmaking/join",
                         json={"QoS": 10, "ProfileId": f"{MatchmakingUser.guid}"}, verify=False, timeout=10)

    @task
    def matchmaking_get_session(self):
        self.matchmaking_join()
        while True:
            self.client.get(f"http://127.0.0.1:90/v1/matchmaking/{MatchmakingUser.guid}/session", verify=False,
                            timeout=10)

    def on_start(self):
        MatchmakingUser.guid = str(uuid.uuid4())


class MatchmakingUserLeave(HttpUser):
    wait_time = between(1, 5)
    guid = ""

    def matchmaking_leave_session(self):
        self.client.post("http://127.0.0.1:90/v1/matchmaking/leave",
                         json={"ProfileId": f"{MatchmakingUser.guid}"}, verify=False, timeout=10)

    def matchmaking_join(self):
        self.client.post("http://127.0.0.1:90/v1/matchmaking/join",
                         json={"QoS": 10, "ProfileId": f"{MatchmakingUser.guid}"}, verify=False, timeout=10)

    @task
    def matchmaking_get_session(self):
        self.matchmaking_join()
        time.sleep(2)
        self.matchmaking_leave_session()

    def on_start(self):
        MatchmakingUser.guid = str(uuid.uuid4())
