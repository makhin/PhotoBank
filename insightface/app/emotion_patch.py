import torch.serialization as serialization
import timm.models.efficientnet

serialization.add_safe_globals([timm.models.efficientnet.EfficientNet])