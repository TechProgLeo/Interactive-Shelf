#include <WiFi.h>
#include <WebServer.h>
#include <FastLED.h>

// ტომას შენ მოგიტყან დედის ლაჯი
// Network credentials
const char* ssid = "DWD_INT";
const char* password = "1409DWDintern++";

// Static IP configuration
IPAddress local_IP(192, 168, 160, 112);      // Static IP for the ESP32
IPAddress gateway(192, 168, 160, 254);      // Gateway (router IP)
IPAddress subnet(255, 255, 255, 0);         // Subnet mask
IPAddress primaryDNS(192, 168, 160, 1);     // DNS server

// Web server
WebServer server(80);

// LED configuration
#define NUM_LEDS  12
#define LED_PIN   16
CRGB leds[NUM_LEDS];

// Command queue
String commandQueue[20];
int queueIndex = 0;
bool isProcessing = false;

// Timing for LED updates
unsigned long lastUpdateTime = 0;
unsigned long delayTime = 5000;  // 5 seconds

// Segment structure for products
struct Segment {
    int startIndex;
    int endIndex;
    unsigned long turnWhiteTime;
    bool isRed;
};

Segment segments[6];  // Assuming 6 products

void setup() {
    // Disconnect WiFi and set the mode
    WiFi.disconnect();
    WiFi.mode(WIFI_STA);  // STA mode (client mode)

    // Set static IP configuration and hostname
    if (!WiFi.config(local_IP, gateway, subnet, primaryDNS)) {
        Serial.println("Failed to configure Static IP");
    }
    WiFi.setHostname("Pick-by-Light-Shelf4");

    // Start WiFi connection
    WiFi.begin(ssid, password);
    
    // Wait for WiFi to connect
    while (WiFi.status() != WL_CONNECTED) {
        delay(1000);
        Serial.println("Connecting to WiFi...");
    }

    // Output the ESP32 IP address
    Serial.println("Connected to WiFi");
    Serial.print("ESP32 IP Address: ");
    Serial.println(WiFi.localIP());

    // Start the web server
    server.on("/lightup", handleLightUp);
    server.begin();

    // Initialize FastLED
    FastLED.addLeds<WS2812B, LED_PIN, GRB>(leds, NUM_LEDS);
    FastLED.setBrightness(255);

    // Initialize segments for products
    for (int i = 0; i < 6; i++) {
        segments[i].startIndex = i * 2;
        segments[i].endIndex = segments[i].startIndex + 2;
        segments[i].turnWhiteTime = 0;
        segments[i].isRed = false;
    }

    // Set all LEDs to white initially
    for(int i = 0; i < NUM_LEDS; i++) {
        leds[i] = CRGB::White;
    }
    FastLED.show();
}

void loop() {
    // Handle client requests
    server.handleClient();

    // Process the command queue asynchronously
    processCommandQueue();

    // Update LED states
    updateLEDs();
}

void processCommandQueue() {
    // Process the next command if the queue is not empty and not already processing
    if (queueIndex > 0 && !isProcessing) {
        isProcessing = true;
        processCommand();
    }
}

// Handles the /lightup request
void handleLightUp() {
    if (server.hasArg("product")) {
        String product = server.arg("product");

        // Queue command if space is available
        if (queueIndex < 10) {
            commandQueue[queueIndex++] = product;
            server.send(200, "text/plain", "Command Queued");
        } else {
            server.send(400, "text/plain", "Queue Full");
        }
    } else {
        server.send(400, "text/plain", "Bad Request");
    }
}

// Processes the next command in the queue
void processCommand() {
    if (queueIndex > 0) {
        String command = commandQueue[0];

        // Light up LEDs for the given product
        lightUpLEDs(command);

        // Shift the queue
        for (int i = 1; i < queueIndex; i++) {
            commandQueue[i-1] = commandQueue[i];
        }
        queueIndex--;

        isProcessing = false;
    }
}

// Lights up the LEDs based on the product command
void lightUpLEDs(String command) {
    int productNumber = command.substring(7).toInt();  // Extract number from "productX"
    
    if (productNumber >= 1 && productNumber <= 6) {  // Check for valid product number
        int segmentIndex = productNumber - 1;

        // Set LEDs to red for the corresponding product
        for (int i = segments[segmentIndex].startIndex; i < segments[segmentIndex].endIndex; i++) {
            leds[i] = CRGB::Red;
        }
        FastLED.show();

        // Set the timer to turn them back to white
        segments[segmentIndex].turnWhiteTime = millis() + delayTime;
        segments[segmentIndex].isRed = true;
    }
}

// Updates LEDs based on the timer
void updateLEDs() {
    unsigned long currentTime = millis();

    // Check each segment to turn LEDs back to white
    for (int i = 0; i < 6; i++) {
        if (segments[i].isRed && currentTime >= segments[i].turnWhiteTime) {
            for (int j = segments[i].startIndex; j < segments[i].endIndex; j++) {
                leds[j] = CRGB::White;
            }
            FastLED.show();

            segments[i].isRed = false;
        }
    }
}
