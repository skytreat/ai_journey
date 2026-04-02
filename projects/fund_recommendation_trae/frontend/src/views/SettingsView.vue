<template>
  <div class="settings-view">
    <el-card shadow="hover">
      <template #header>
        <div class="card-header">
          <span>系统设置</span>
        </div>
      </template>
      
      <div class="settings-content">
        <el-tabs v-model="activeTab">
          <el-tab-pane label="评分权重" name="weights">
            <el-form :model="weightForm" label-width="150px">
              <el-form-item label="收益权重">
                <el-slider v-model="weightForm.returnWeight" :min="0" :max="1" :step="0.01" show-input></el-slider>
              </el-form-item>
              <el-form-item label="风险权重">
                <el-slider v-model="weightForm.riskWeight" :min="0" :max="1" :step="0.01" show-input></el-slider>
              </el-form-item>
              <el-form-item label="风险调整收益权重">
                <el-slider v-model="weightForm.riskAdjustedReturnWeight" :min="0" :max="1" :step="0.01" show-input></el-slider>
              </el-form-item>
              <el-form-item label="排名权重">
                <el-slider v-model="weightForm.rankingWeight" :min="0" :max="1" :step="0.01" show-input></el-slider>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="saveWeights">保存权重配置</el-button>
              </el-form-item>
            </el-form>
          </el-tab-pane>
          
          <el-tab-pane label="API设置" name="api">
            <el-form :model="apiForm" label-width="150px">
              <el-form-item label="API基础URL">
                <el-input v-model="apiForm.baseUrl" placeholder="请输入API基础URL"></el-input>
              </el-form-item>
              <el-form-item label="API超时时间">
                <el-input-number v-model="apiForm.timeout" :min="1000" :max="60000" :step="1000"></el-input-number>
                <span style="margin-left: 10px">毫秒</span>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="saveApiSettings">保存API设置</el-button>
              </el-form-item>
            </el-form>
          </el-tab-pane>
          
          <el-tab-pane label="系统信息" name="system">
            <el-descriptions :column="1" border>
              <el-descriptions-item label="系统版本">1.0.0</el-descriptions-item>
              <el-descriptions-item label="后端API版本">1.0.0</el-descriptions-item>
              <el-descriptions-item label="数据库版本">SQLite 3.x</el-descriptions-item>
              <el-descriptions-item label="前端框架">Vue 3 + Element Plus</el-descriptions-item>
              <el-descriptions-item label="后端框架">.NET 8 Web API</el-descriptions-item>
            </el-descriptions>
          </el-tab-pane>
        </el-tabs>
      </div>
    </el-card>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import axios from 'axios'

export default {
  name: 'SettingsView',
  setup() {
    const activeTab = ref('weights')
    const weightForm = ref({
      returnWeight: 0.3,
      riskWeight: 0.2,
      riskAdjustedReturnWeight: 0.3,
      rankingWeight: 0.2
    })
    const apiForm = ref({
      baseUrl: 'http://localhost:5026/api',
      timeout: 10000
    })
    
    const loadWeights = async () => {
      try {
        const response = await axios.get('http://localhost:5026/api/favorites/scores/weights')
        weightForm.value = response.data
      } catch (error) {
        console.error('Error loading weights:', error)
        // 使用默认值
      }
    }
    
    const saveWeights = async () => {
      try {
        await axios.put('http://localhost:5026/api/favorites/scores/weights', weightForm.value)
        ElMessage.success('权重配置保存成功')
      } catch (error) {
        console.error('Error saving weights:', error)
        ElMessage.error('权重配置保存失败')
      }
    }
    
    const saveApiSettings = () => {
      // 保存API设置到本地存储
      localStorage.setItem('apiSettings', JSON.stringify(apiForm.value))
      ElMessage.success('API设置保存成功')
    }
    
    const loadApiSettings = () => {
      const savedSettings = localStorage.getItem('apiSettings')
      if (savedSettings) {
        apiForm.value = JSON.parse(savedSettings)
      }
    }
    
    onMounted(() => {
      loadWeights()
      loadApiSettings()
    })
    
    return {
      activeTab,
      weightForm,
      apiForm,
      saveWeights,
      saveApiSettings
    }
  }
}
</script>

<style scoped>
.settings-view {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.settings-content {
  margin-top: 20px;
}

.el-slider {
  width: 300px;
}
</style>