import random
# choose 60 times "vr" or "screen" and save to the file choices.txt
with open("choices.txt", "w") as f:
    for i in range(60):
        f.write(random.choice(["vr", "screen"]))
        f.write('\n')